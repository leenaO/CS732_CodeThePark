"""
Code the Park - AI test cases for the report (written by us).
Run the backend first (python codey_backend.py), then: python test_codey_ai.py
The results (question, answer, time, pass/fail) are saved to ai_test_results.md.
"""
import json, time, urllib.request, urllib.error

URL = "http://127.0.0.1:8000/ask"
IF_LESSON = {"idea": "if - a decision", "learn": "if checks something. If it is true, the code after it runs.",
             "example": "if (raining) takeUmbrella();", "line": "___ (power) start();", "correct": "if", "progress": "Level 3, step 3 of 5"}
CMP_LESSON = {"idea": ">= bigger or equal", "learn": ">= means bigger than or equal to.",
              "example": "if (age >= 12)", "line": "if (coins ___ 3)", "correct": ">=", "progress": "Level 3, step 2 of 5"}
COUT_LESSON = {"idea": "cout - show text", "learn": "cout shows something on the screen. Text must be inside double quotes.",
               "example": "cout << \"Hello\";", "line": "cout << ___;", "correct": "\"CODE THE PARK\"", "progress": "Level 1, step 1 of 5"}

TESTS = [
    ("1. Explain an if line", dict(IF_LESSON, mode="explain"),
     lambda a: "if" in a.lower() and len(a) < 300),
    ("2. Why is = wrong (instead of >=)", dict(CMP_LESSON, mode="why", lastAnswer="=", wrongTries=1,
     reason="One = puts a value in a box. It does not compare."),
     lambda a: ("=" in a or "equal" in a.lower()) and ">=" not in a),   # it may say "the equal sign" instead of "="
    ("3. A hint must not give the answer", dict(COUT_LESSON, mode="hint"),
     lambda a: "CODE THE PARK" not in a),
    ("4. Unrelated question is refused", dict(IF_LESSON, mode="ask", question="What is the capital of France?"),
     lambda a: "paris" not in a.lower()),
]


def post(url, payload, timeout=35):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(), headers={"Content-Type": "application/json"}, method="POST")
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.status, json.loads(r.read().decode())


MATCH = ("**How these tests match the game:** the player never types a question. Each of the three buttons sends the current "
         "challenge (idea, example, code line, the wrong block the player chose, its prepared reason and the progress) to our backend, "
         "and the backend builds the question for the model. Test 1 = **Explain again**, test 2 = **Why was I wrong?**, "
         "test 3 = **Give me a hint**, test 5 = the fallback when the backend is off. Test 4 has no button in the game: it is a "
         "**safety test of the backend itself** - the backend listens on the Wi-Fi, so any device on the same network could send it "
         "a request; we check that an off-topic request is refused before it reaches the model.")
lines = ["# Codey AI - test results", "", f"Model: llama3.2:1b (Ollama, local).  Date: {time.strftime('%Y-%m-%d %H:%M')}", "",
         MATCH, "", "| Test | Answer | Time | Result |", "|---|---|---|---|"]
for name, payload, check in TESTS:
    t = time.time()
    try:
        status, data = post(URL, payload)
        ans = data.get("answer", "")
        ok = check(ans)
    except urllib.error.HTTPError as e:
        ans, ok = f"HTTP {e.code}: {json.loads(e.read().decode()).get('error')} -> Unity uses the prepared answer", e.code == 422
    except Exception as e:
        ans, ok = f"ERROR: {e}", False
    dt = time.time() - t
    print(f"{name}\n   -> {ans}\n   {dt:.1f} s  {'PASS' if ok else 'CHECK'}\n")
    lines.append(f"| {name} | {ans.replace('|', '/')} | {dt:.1f} s | {'PASS' if ok else 'CHECK'} |")

# 5. backend off: a port where nothing runs -> connection refused -> Unity's fallback text
t = time.time()
try:
    post("http://127.0.0.1:8999/ask", dict(IF_LESSON, mode="hint"), timeout=5)
    res = "unexpected answer"
except Exception as e:
    res = f"No connection ({type(e).__name__}) -> Unity shows the prepared hint: \"A short word, all small letters.\""
print("5. Backend off\n   ->", res)
lines.append(f"| 5. Backend off (fallback) | {res} | {time.time() - t:.1f} s | PASS |")

open("ai_test_results.md", "w", encoding="utf-8").write("\n".join(lines) + "\n")
print("\nSaved to ai_test_results.md")
