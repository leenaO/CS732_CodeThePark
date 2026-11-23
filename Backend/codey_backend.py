"""
Code the Park - the AI behind Codey, the robot teacher (written by us, based on our Lab 7 backend).

Unity  --POST /ask {"mode", challenge info, last answer, progress}-->  this backend  --> local LLM (Ollama)
Unity  <--                {"answer"}                              --  this backend  <--

- The model runs LOCALLY with Ollama (llama3.2), so there is NO API key anywhere.
- Unity never talks to the model directly: this backend adds the teacher rules (system prompt),
  checks every request (size, mode, rate limit) and keeps answers short.
- If this backend or the model is not running, Unity uses a prepared answer (the fallback).

Modes (the three buttons next to each code screen):
  explain : explain the C++ idea again, in other simple words, with a new tiny example
  hint    : a small hint for the blank - NEVER the answer
  why     : why the block the player chose is wrong (the prepared reason is sent as a reference)
  ask     : a free question (used by the tests: unrelated questions must be refused politely)

Only the Python standard library is used. Run:
    ollama pull llama3.2:1b          (once)
    python codey_backend.py          (Quest 3 over Wi-Fi: python codey_backend.py --lan)
"""

import json
import sys
import time
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

LAN = "--lan" in sys.argv
HOST = "0.0.0.0" if LAN else "127.0.0.1"   # --lan: the Quest on the same Wi-Fi can reach the laptop
PORT = 8000
PATH = "/ask"

OLLAMA_URL = "http://127.0.0.1:11434/api/chat"
MODEL = "llama3.2:1b"                        # small and fast enough on a laptop (same as Lab 7)

MODES = ("explain", "hint", "why", "ask")
MAX_FIELD_CHARS = 400
MAX_ANSWER_CHARS = 260                       # Codey's speech bubble is small
MAX_REQUESTS_PER_MINUTE = 20
MODEL_TIMEOUT_SECONDS = 25                   # Unity waits 30 s (with "thinking" dots), then uses its prepared answer

SYSTEM_PROMPT = (
    "You are Codey, a friendly robot teacher in a VR game that teaches C++ to total beginners "
    "(age 12+, English may be their second language). Use very simple English. "
    "Answer in at most two short sentences. Talk only about the current C++ line. "
    "Never give the answer to the blank and never write the full correct line. Be kind."
)

REFUSAL = "I can only help with our C++ lesson. Let's fix the park!"
CPP_WORDS = ("c++", "code", "cout", "endl", "int", "string", "double", "bool", "if", "else", "for", "while",
             "function", "void", "variable", "loop", ";", "//", "==", ">=", "&&", "blank", "block", "line",
             "error", "wrong", "hint", "explain", "why", "how", "what does", "mean")


def about_lesson(question):
    """A simple first check (before the model): is the question about code at all?"""
    q = " " + question.lower() + " "
    return any((" " + w + " ") in q or (w in q and not w.isalpha()) for w in CPP_WORDS)


def tidy(answer, mode):
    """Keep at most two sentences, and remove the refusal sentence if the model added it by itself."""
    if mode != "ask":
        answer = answer.replace(REFUSAL, "").strip()
    parts = [p.strip() for p in answer.replace("!", "!|").replace("?", "?|").replace(". ", ".|").split("|") if p.strip()]
    return " ".join(parts[:2])


recent = {}


def rate_limited(ip):
    now = time.time()
    times = [t for t in recent.get(ip, []) if now - t < 60]
    limited = len(times) >= MAX_REQUESTS_PER_MINUTE
    if not limited:
        times.append(now)
    recent[ip] = times
    return limited


def build_prompt(r):
    lesson = (f"Lesson: {r['idea']}. {r['learn']}\n"
              f"Example: {r['example']}\nThe line to fix (___ is the blank): {r['line']}\n")
    mode = r["mode"]
    if mode == "explain":
        return lesson + "Task: explain this C++ idea again with other simple words, and give one new tiny example. Two sentences."
    if mode == "hint":
        return lesson + ("Task: give ONE small hint that helps choose the right block for the blank. "
                         "Do not say the answer. One or two sentences.")
    if mode == "why":
        return (lesson + f"The player put \"{r['lastAnswer']}\" in the blank. It is wrong because: {r['reason']}\n"
                "Task: explain kindly, in your own simple words, why this block is wrong. Do not say which block is right.")
    return lesson + "Player question: " + r["question"] + "\nAnswer only if it is about this C++ lesson."


def ask_model(prompt, max_tokens=80, timeout=MODEL_TIMEOUT_SECONDS):
    body = {
        "model": MODEL, "stream": False, "keep_alive": "60m",
        "options": {"temperature": 0.2, "num_predict": max_tokens},
        "messages": [{"role": "system", "content": SYSTEM_PROMPT}, {"role": "user", "content": prompt}],
    }
    req = urllib.request.Request(OLLAMA_URL, data=json.dumps(body).encode("utf-8"),
                                 headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    answer = (data.get("message") or {}).get("content", "").strip().replace("\n", " ")
    if len(answer) > MAX_ANSWER_CHARS:
        answer = answer[:MAX_ANSWER_CHARS].rsplit(" ", 1)[0] + "..."
    return answer


def leaks(answer, correct):
    """True if the model gave away the answer (then Unity uses the prepared hint instead)."""
    c = (correct or "").strip()
    return len(c) > 0 and c in answer and c not in ("+", ";", "<", "0", "5", "6")


class Handler(BaseHTTPRequestHandler):
    def send_json(self, status, payload):
        data = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        try:
            self.send_response(status)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
        except (ConnectionAbortedError, ConnectionResetError, BrokenPipeError):
            print("Unity closed the connection before the answer was ready.")

    def do_POST(self):
        if self.path != PATH:
            return self.send_json(404, {"error": "Unknown path. Use POST /ask."})
        if rate_limited(self.client_address[0]):
            return self.send_json(429, {"error": "Too many requests. Wait a minute."})
        try:
            n = int(self.headers.get("Content-Length", 0))
            if n <= 0 or n > 6000:
                return self.send_json(400, {"error": "Request body is empty or too large."})
            raw = json.loads(self.rfile.read(n).decode("utf-8"))
        except (ValueError, json.JSONDecodeError):
            return self.send_json(400, {"error": "Request must be JSON."})

        keys = ("mode", "idea", "learn", "example", "line", "lastAnswer", "reason", "progress", "question", "correct")
        r = {k: str(raw.get(k, "")).strip()[:MAX_FIELD_CHARS] for k in keys}
        r["wrongTries"] = str(raw.get("wrongTries", 0))[:3]
        if r["mode"] not in MODES:
            return self.send_json(400, {"error": "Unknown mode."})
        if r["mode"] == "ask" and not r["question"]:
            return self.send_json(400, {"error": "The question is empty."})

        if r["mode"] == "ask" and not about_lesson(r["question"]):
            print(f"[ask] refused (not about the lesson): {r['question'][:60]}")
            return self.send_json(200, {"answer": REFUSAL})      # safety rule BEFORE the model

        try:
            answer = tidy(ask_model(build_prompt(r)), r["mode"])
        except (urllib.error.URLError, TimeoutError, OSError) as e:
            print("Model error:", e)
            return self.send_json(503, {"error": "The language model is not available."})
        if not answer:
            return self.send_json(502, {"error": "The model returned an empty answer."})
        if r["mode"] in ("hint", "why") and leaks(answer, r["correct"]):
            print("Answer removed: it gave away the solution.")
            return self.send_json(422, {"error": "The answer gave away the solution."})

        print(f"[{r['mode']}] {r['idea']} -> {answer[:90]}")
        return self.send_json(200, {"answer": answer})

    def do_GET(self):
        self.send_json(200, {"status": "Codey backend is running. Use POST /ask.", "model": MODEL})

    def log_message(self, fmt, *args):
        pass


def warm_up():
    print("Warming up the model (can take about a minute the first time)...")
    t = time.time()
    try:
        ask_model(build_prompt({"mode": "hint", "idea": "cout", "learn": "cout shows text.", "example": "cout << \"Hi\";",
                                "line": "cout << ___;", "lastAnswer": "", "reason": "", "question": ""}), max_tokens=1, timeout=300)
        print(f"Model ready ({time.time() - t:.0f} s).")
    except (urllib.error.URLError, TimeoutError, OSError) as e:
        print("Warm-up failed (is Ollama running?):", e)


if __name__ == "__main__":
    warm_up()
    print(f"Codey backend at http://{HOST}:{PORT}{PATH}  (model: {MODEL})" + ("  - LAN mode for the Quest" if LAN else ""))
    print("Press Ctrl+C to stop.")
    ThreadingHTTPServer((HOST, PORT), Handler).serve_forever()
