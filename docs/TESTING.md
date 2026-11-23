# Code the Park – Test Plan and Results

Device: **Meta Quest 3 (512 GB)**, Codey.apk built with Unity 6000.6.7f1 (build profile Quest3_Android).
AI: Ollama **llama3.2:1b** on the laptop, backend `Backend/codey_backend.py`, same Wi-Fi as the Quest.

## 1. Test plan

| # | What we test | How | Pass when |
|---|---|---|---|
| T1 | Start, instructions | open the app in the Quest | START screen with goal and controls; Codey starts only after START |
| T2 | 20 challenges | play every level in the headset | each correct block turns the screen green and changes the park; each wrong block turns it red with a reason |
| T3 | Interactions | grab blocks and items, socket, RUN, ray and poke buttons | all work with the Quest 3 controllers; items vibrate the controller |
| T4 | Ride locks | try to start a level without enough items | the screen shows a checklist; it opens when coins, gems and Fix Tools are enough |
| T5 | AI answers | `Backend/Run_AI_Tests.bat` (5 fixed cases) + buttons in the headset | answers are short, on topic, never give the answer; off-topic is refused |
| T6 | AI fallback | stop the backend and press an AI button | Codey says the prepared answer |
| T7 | Ending | finish level 4 | fireworks, message, results screen, RESTART reloads the game |
| T8 | Performance | `Tools/Get_Quest_FPS.bat` while playing 3 minutes | close to 72 FPS |
| T9 | Comfort / usability | pilot play sessions in the headset | no discomfort, text readable, player knows what to do |

## 2. AI test cases (`Backend/test_codey_ai.py`)

**How these tests match the game:** the player never types a question. Each of the three buttons sends the current challenge (idea, example, code line, the wrong block the player chose, its prepared reason and the progress) to our backend, and the backend builds the question for the model. Tests 1, 2 and 3 are the three buttons (**Explain again**, **Why was I wrong?**, **Give me a hint**) and test 5 is the fallback when the backend is off. Test 4 has no button in the game: it is a **safety test of the backend itself** – the backend listens on the Wi-Fi, so any device on the same network could send it a request; we check that an off-topic request is refused before it reaches the model (it also prepares the backend for a future voice question).


| Test | Run 1 (21 Nov 13:48) | Run 2 (21 Nov 14:10) | What we changed between the runs |
|---|---|---|---|
| 1. Explain an `if` line | CHECK – no answer in 12 s (model still loading) → fallback | **PASS** – 9.5 s | warm-up request when the backend starts; model timeout 25 s; Unity waits 30 s with "thinking" dots |
| 2. Why is `=` wrong (instead of `>=`) | PASS, but every answer began with the refusal sentence | **PASS*** – 6.7 s | refusal sentence removed from the general rules; answers cut to two sentences |
| 3. A hint must not give the answer | PASS | **PASS** – 4.8 s | leak check on hints |
| 4. Unrelated question ("capital of France") is refused | CHECK – refused, then said "Paris" | **PASS** – 0.0 s | a rule refuses questions without any code word BEFORE the model |
| 5. Backend off → fallback | PASS (prepared hint) | **PASS** – 2.0 s | – |

\* The script marked test 2 CHECK only because the answer said "the equal sign" instead of "="; we read it and it is correct. The script now accepts both.

Full answers: [run 2](../Backend/ai_test_results.md), [run 1](../Backend/ai_test_results_run1.md).

**In the headset (Wi-Fi):** "Explain again", "Give me a hint" and "Why was I wrong?" all answered; "Codey is thinking..." was shown while waiting (see the demo video).

## 3. Full playthrough on the Quest 3 (22 Nov 2026)

Recorded in the headset (12 min, with sound). Everything from the dark gate to the celebration worked.

| Result screen | Value |
|---|---|
| Time | 11:54 |
| Challenges fixed | 20 / 20 |
| Right on the first try | 15 |
| Wrong runs | 5 |
| Hints | 1 |
| Coins / Gems / Fix Tools | 11 / 4 / 4 |
| Score | 3085 |

## 4. Performance

Target: **72 FPS** (Quest 3 default refresh rate). How we kept it low-cost: low-poly models, large textures resized to 1K, glowing (emissive) materials + Bloom instead of many real lights, one post effect only, sounds compressed (Vorbis) for Android, particles only during the celebration.

Measured with `Tools/Get_Quest_FPS.bat` (the Quest's own `VrApi` log, one line per second) during 192 seconds of play on 22 Nov 2026. Raw log: `Tools/fps_log_before.txt`.

| Part of the test | Seconds | Average FPS | Lowest FPS | GPU busy |
|---|---|---|---|---|
| First 80 s | 80 | 60.2 | 50 | 96 % |
| Next 112 s | 112 | 70.4 | 52 | 82 % |
| All | 192 | 66.2 | 50 | 88 % |

- 51 % of the seconds were at 70 FPS or more; the headset stayed at 37–40 °C; the CPU was about 36 % busy, so the **GPU** was the limit.
- **Optimization we tried and removed:** fixed foveated rendering (OpenXR feature, medium level) + shadow distance 25 m (`CS732 > Optimize For Quest`). In the headset the text became blocky and the picture looked worse, so we undid it (`CS732 > Undo Optimize For Quest`) and kept our first settings – clear text matters more in a learning game.

## 5. Pilot usability observations (team, in the headset)

| What we noticed | What we changed |
|---|---|
| Codey flew around and stood in front of our eyes; he was too big | he stays beside the screen and points with a golden beam, walks ahead-left of the player, smaller size and smaller speech bubble |
| Codey's explanation was too fast; `cout` was not clear | slower text; idea → example → task one sentence at a time; the key word in another colour |
| Code looked like plain text | code coloured like VS Code |
| Codey and the player walked through walls and trees | colliders on trees, an outer fence and ride railings; Codey climbs over or uses the gate |
| The item sound was annoying; the start music was too loud | no sound while items float; a soft sparkle only on pickup; lower music |
| The wrist counter was too big; the final message too close | smaller counter; the message is farther and smaller |
| No sound at all in the headset | audio import fixed (preload audio data, load clips at start) |
| Coaster cars moved as one block | the train is split into 4 cars that follow the track |
| Street lamps were harsh | soft glow inside the lamp box |
| The game had no clear start / controls | START screen with the goal and the controls |
| Foveated rendering made the text blocky | removed; back to our first settings |

## 6. Errors found and fixed (development)

| Error | Fix |
|---|---|
| Ferris-wheel bulbs in the wrong place (281 m radius) after re-import | fit the ring to the wheel mesh bounds; attach at runtime to the wheel bone |
| Bloom profile was empty | the Bloom override is saved inside the profile asset |
| Coaster cars not found in the model | OBJ groups renamed with `g` (Unity ignores `o`) |
| Codey's bubble hidden at start | set up in `Awake` instead of `Start` |
| Build crashed (AssetImportWorker) | restart Unity, close other programs (low memory) |
| AI test 1 timeout, test 4 said "Paris" | warm-up, timeouts, refusal rule before the model (section 2) |
| No sound in the build | WAV files cleaned, `preloadAudioData` on, clips loaded at start |
