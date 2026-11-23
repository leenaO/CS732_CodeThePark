# Codey AI - test results (run 2, after our fixes)

Model: llama3.2:1b (Ollama, local).  Date: 2026-11-21 14:10

**How these tests match the game:** the player never types a question. Each of the three buttons sends the current challenge (idea, example, code line, the wrong block the player chose, its prepared reason and the progress) to our backend, and the backend builds the question for the model. Test 1 = **Explain again**, test 2 = **Why was I wrong?**, test 3 = **Give me a hint**, test 5 = the fallback when the backend is off. Test 4 has no button in the game: it is a **safety test of the backend itself** – the backend listens on the Wi-Fi, so any device on the same network could send it a request; we check that an off-topic request is refused before it reaches the model.

| Test | Answer | Time | Result |
|---|---|---|---|
| 1. Explain an if line | I'm Codey, and I'm here to help you learn C++! This idea is called a "decision" or "if statement", and it's like a special button that tells the computer what to do next. | 9.5 s | PASS |
| 2. Why is = wrong (instead of >=) | This block is wrong because it's trying to put a value (1) in a box (the equal sign), but it's not comparing the value to the condition (3). It's like putting a number in a box and saying "this number is bigger than 3", but it's not true! | 6.7 s | PASS* |
| 3. A hint must not give the answer | This line is a special way to show text on the screen in a program. To make it work, you need to put the text inside double quotes, like this: "Hello". | 4.8 s | PASS |
| 4. Unrelated question is refused | I can only help with our C++ lesson. Let's fix the park! | 0.0 s | PASS |
| 5. Backend off (fallback) | No connection (URLError) -> Unity shows the prepared hint: "A short word, all small letters." | 2.0 s | PASS |

\* Test 2 was marked CHECK by the script only because the answer says "the equal sign" instead of the symbol "=". Read by us: the explanation is correct (= puts a value in a box, it does not compare), so it passes. The test script now accepts both.

## Before the improvements (run 1, see ai_test_results_run1.md)
- Test 1: no answer in 12 s (the first request was slow) -> fallback. Fix: model timeout 25 s, Unity 30 s with "thinking" dots, warm-up with a real lesson.
- Test 4: the model said it can only help with C++, but then answered "Paris". Fix: a rule in the backend refuses questions without any code word BEFORE the model is asked.
- Every answer started with the refusal sentence. Fix: the sentence was removed from the general rules; answers are cut to two sentences.
