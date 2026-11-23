# Code the Park – Learn C++ in VR

**CS 732 – Virtual Reality · Final Project · King Abdulaziz University · Fall 2026**
Instructor: Dr. Wadee Alhalabi

| Team member | Student ID | Main role |
|---|---|---|
| Shahd Ibrahim Alqaan | 2701566 | Design and development of the whole game: Unity / Quest 3 setup and builds, park environment, VR interactions, the 20 C++ lessons, Codey and the AI backend + tests, audio, start / results screens, testing, video, report and documentation |
| Lena Alobaid | 2701916 | Reviewed the C++ lessons and the sounds, took part in the daily team meetings, published the GitHub repository and the release (APK + demo video), final review of the repository, report and video |

![Code the Park](docs/images/02_code_screen.jpg)

## 1. The idea

At night an amusement park is dark and closed because **its code is broken**. The player is the new park programmer and has never written code before. With **Codey**, a friendly robot teacher, the player fixes **20 short lines of C++ in 4 levels**. Every correct line changes the park: a sign lights up, the gate opens, a ride starts to turn. The last line, `celebrate();`, starts fireworks and the message *"You will be a great programmer!"*.

- **Target user:** total beginners (12+) and first-time VR users
- **Player role:** the park programmer
- **Objective:** fix all 20 lines so every ride works
- **Platform:** Meta Quest 3 (standalone Android build)

| Level | C++ ideas (5 challenges each) | What changes in the park |
|---|---|---|
| 1. Park Gate – Basics | `cout` text, `cout` number, `endl`, `//`, `;` | neon name, year, bulbs, sun wheel, the gate opens |
| 2. Carousel – Variables | `int`, `string`, `double`, `+`, `++` | lights, name, ticket price, slow turn, full power + a speed playground (0 / 3 / 6) |
| 3. Ferris Wheel – Decisions | `bool`, `>=`, `if`, `else`, `&&` | cabins light, the wheel turns, wheel bulbs, full power |
| 4. Roller Coaster – Loops, Functions | `for`, `while`, define a function, call `celebrate()` | bulbs, the 4-car train runs on the track, light show, celebration |

**Start:** a START screen shows the goal and the controls; after **START**, Codey introduces himself and leads the player to the first screen.

**Each challenge – Learn, Try, Understand:** Codey explains the idea with a real-life example (the key word is coloured) → the player grabs one of **3 code blocks** (1 correct, 2 common beginner mistakes, coloured like VS Code), puts it in the yellow slot and presses **RUN** → green screen and the park changes, or red screen and Codey explains *why* this block is wrong.

**Collectibles:** 40 items (6 coins, 2 gems, 2 Fix Tools per level). Each ride needs enough of all three: Carousel 3/1/1, Ferris Wheel 6/2/2, Roller Coaster 9/3/3 (coins / gems / tools, counted in total). The left-wrist counter turns into real C++ (`int coins = 5;`, `coins++`) after the player learns it.

**Scoring:** +100 per solved challenge, +50 first try, −10 wrong run, −5 hint, +10 coin, +50 gem, +20 Fix Tool. No time limit – time is only counted. The results screen shows time, challenges fixed, first tries, wrong runs, items and score, with **RESTART**.

## 2. Screenshots (recorded in the Quest 3)

| | |
|---|---|
| ![start screen](docs/images/00_start_screen.jpg) START screen with the controls | ![start](docs/images/01_start_gate.jpg) The dark gate – Codey says hello |
| ![AI](docs/images/03_ai_answer.jpg) Codey answers with AI | ![code](docs/images/02_code_screen.jpg) A Level 1 code screen |
| ![correct](docs/images/04_correct.jpg) A correct answer | ![carousel](docs/images/05_carousel.jpg) Level 2 – Carousel |
| ![wheel](docs/images/06_ferris_wheel.jpg) Level 3 – Ferris wheel | ![celebration](docs/images/07_celebration.jpg) `celebrate();` |

![results](docs/images/08_results.jpg)

**Demo video (5 min) and the APK:** see **Releases** (right side of this page) – `CodeThePark_Demo_Short.mp4` and `Codey.apk` (too large for normal Git files).

## 3. Software and versions

| Item | Version |
|---|---|
| Unity | **6000.6.7f1** (Unity 6.6), Universal Render Pipeline 17.6 |
| XR | XR Interaction Toolkit **3.6.1** (+ Starter Assets, XR Device Simulator), OpenXR 1.18.0, Unity OpenXR: Meta 2.7.0, XR Plug-in Management 4.7.0 |
| Other packages | Input System 1.20.1, TextMeshPro (uGUI 2.6.0), glTFast 6.21.0 |
| Headset | Meta Quest 3, build profile **Quest3_Android** (Android, OpenXR + Meta Quest Support) |
| AI | Python 3 (standard library only) + **Ollama** with **llama3.2:1b** |

## 4. Open and run the project

1. Install Unity **6000.6.7f1** with **Android Build Support** (OpenJDK + SDK/NDK).
2. Clone this repository and open the folder in Unity Hub (*Add project from disk*). The first import takes a few minutes.
3. Open the scene `Assets/Scenes/CodeThePark.unity`.
4. *(Optional)* Menu **CS732 > Update Park (do everything)** rebuilds the lights, items, stations, sounds and screens.
5. **Fireworks sound:** this file is not in the repository (the Pixabay licence does not allow sharing it as a file). Download *"Sound effects of fireworks (Diwali celebration)"* by Pankaj Yadav (Pixabay, track 421220) and save it as `Assets/Audio/Celebration/fireworks_celebration.wav`. Without it the game still works (only the fireworks are silent).

**Play in the Unity editor (no headset):** **CS732 > Device Simulator ON (keyboard + mouse)**, then Play. Test keys: `Enter` = START / big button, `F1 F2 F3` = put block 1 / 2 / 3 in the slot, `F5 F6 F7` = collect the nearest coin / gem / Fix Tool, `F10 F11 F12` = the three AI buttons.

**Build for the Quest 3:** turn on Developer Mode, connect with USB-C and allow USB debugging. **CS732 > Device Simulator OFF**, then *File > Build Profiles > Quest3_Android > Build And Run*. Or install the ready APK from Releases: `adb install -r Codey.apk`. The app appears in *Library > Unknown Sources* as **Code the Park**.

## 5. Controls (also shown on the START screen)

| Control | Action |
|---|---|
| Left thumbstick | walk |
| Right thumbstick | turn (left / right), teleport (push forward) |
| Grip (side button) | grab a code block, coin, gem or Fix Tool (the controller vibrates) |
| Trigger | press buttons with the ray – or touch them with the hand (poke) |
| Left wrist | look at it to see what you collected |

## 6. AI design – Codey

```
Quest 3 (Unity, CodeyAI.cs) --POST /ask {mode, idea, line, last wrong block, progress}--> Backend/codey_backend.py --> Ollama (llama3.2:1b, local)
                            <------------------------------- {"answer"} ----------------------------------------
```

- Three buttons beside every code screen: **Explain again**, **Give me a hint**, **Why was I wrong?**. While waiting, Codey shows *"Codey is thinking..."*.
- **No API key anywhere:** the model runs locally with Ollama; the Quest reaches the laptop over the same Wi-Fi.
- **Teacher rules (system prompt):** very simple English, at most two short sentences, only about the current line, never give the answer.
- **Safety:** off-topic questions are refused by a rule *before* the model; hints that contain the correct block are removed; request size and rate limits (20 per minute); model timeout 25 s (Unity waits 30 s).
- **Fallback:** if the backend or model is off, slow, or leaks the answer, Codey uses the prepared explanation / hint / reason from `ChallengeData.cs`. The game works fully without AI.

**Run the AI:**
1. Install [Ollama](https://ollama.com) and Python 3, then once: `ollama pull llama3.2:1b`
2. Editor: double-click `Backend/Start_Codey_AI.bat`. Quest: `Backend/Start_Codey_AI_for_Quest.bat` – it prints the laptop address; put it in Unity in *GameSystems > Codey AI > Backend Url* (for example `http://192.168.1.20:8000/ask`) and allow the firewall.
3. Tests: `Backend/Run_AI_Tests.bat` → `Backend/ai_test_results.md`.

## 7. Testing

Full details: [docs/TESTING.md](docs/TESTING.md).

- **AI tests (5 cases, 2 runs):** run 1 – 3 PASS, 2 CHECK (the first request timed out; an off-topic question was answered). After the fixes, run 2 – **5 / 5 PASS** ([run 2](Backend/ai_test_results.md), [run 1](Backend/ai_test_results_run1.md)).
- **Full playthrough on the Quest 3:** all 20 challenges, 4 rides, AI buttons and the celebration worked – 20 / 20 fixed, 15 first tries, 5 wrong runs, 1 hint, score 3085, time 11:54.
- **Performance (Quest 3, 192 s):** average 66 FPS (target 72), 70+ FPS in 51 % of the seconds, lowest 50; GPU-bound. Foveated rendering was tested and removed because it made the text blocky. Details in docs/TESTING.md.
- **Usability:** many pilot tests in the headset; each problem we found and how we fixed it is in docs/TESTING.md.

## 8. Project structure

| Folder | What is inside |
|---|---|
| `Assets/Scripts` | 19 game scripts – **all written by us** (game flow, code screens, Codey, AI client, rides, items, sounds, start screen, celebration) |
| `Assets/Editor` | 23 editor helpers – **written by us** – the **CS732** menu that builds the repeated parts of the park |
| `Assets/Samples` | XRI Starter Assets and XR Device Simulator – **ready-made** (Unity) |
| `Assets/Models`, `Assets/Audio`, `Assets/Materials` | third-party models (see credits), our own sounds and materials |
| `Backend` | AI backend, tests and start files – **written by us** |
| `Tools` | `make_sounds.py` (our sound effects and music) and Quest log / FPS / video tools |
| `docs` | testing, progress log, contribution statements, MR extension design, images |

## 9. Known limitations

- The AI needs the laptop running Ollama on the same Wi-Fi; otherwise the prepared answers are used.
- The small 1B model is fast but sometimes gives simple or slightly off answers; answers are kept to two sentences and checked.
- Usability tests were done by the team, not yet with real beginners.
- Mixed reality is a design only: [docs/MR_EXTENSION.md](docs/MR_EXTENSION.md).
- English only.

## 10. Team documentation

- Progress log: [docs/PROGRESS_LOG.md](docs/PROGRESS_LOG.md)
- Individual contribution statements: [docs/CONTRIBUTIONS.md](docs/CONTRIBUTIONS.md)
- The proposal and the technical report are in our Blackboard submission.

## 11. Asset credits

All third-party models and sounds are free (CC0 / CC BY 4.0 / Pixabay licence) and listed with authors, links and licences in **[CREDITS.md](CREDITS.md)**: carousel (Enkarra), Ferris wheel (Yevheniia Lenda), roller coaster (gaartt), Codey robot "BOT NEIL" (OSCAR CREATIVO), lamp post (Memorie), bench (adventurer) – all CC BY 4.0; nature kit (Quaternius) and grass texture (Poly Haven) – CC0; fireworks sound (Pixabay). All other sounds and music were made by us with code. The AI model is Meta's Llama 3.2 1B (Llama 3.2 Community License), run locally with Ollama.
