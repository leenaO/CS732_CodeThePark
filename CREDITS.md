# Code the Park – Asset Credits

| Asset | Author | Source | License | Used for |
|---|---|---|---|---|
| WIP CGMA Merry-go-round carousel | Enkarra | https://sketchfab.com (search: "WIP CGMA Merry-go-round carousel" by Enkarra) | CC Attribution (CC BY 4.0) | Level 2 carousel |
| Old Ferris Wheel | Yevheniia Lenda | https://sketchfab.com/3d-models/old-ferris-wheel-2a2e943624d74d668e5a5bc5cf693843 | CC Attribution (CC BY 4.0) | Level 3 Ferris wheel |
| Stylized Nature MegaKit (Standard) | Quaternius | https://quaternius.com/packs/stylizednaturemegakit.html | CC0 1.0 | Trees, bushes, flowers, grass, rocks |
| Sparse Grass texture | Poly Haven | https://polyhaven.com/textures | CC0 1.0 | Ground (resized to 1K and recoloured to a green lawn by us) |
| Low-Poly Lamp Post | Memorie | https://sketchfab.com/3d-models/low-poly-lamp-post-c466684e819a4428b6d8ed50537615e4 | CC BY 4.0 | Lamp posts |
| Bench_ Park - 14MB | adventurer | https://sketchfab.com/3d-models/bench--park-14mb-6081a54e64a94edda2c444d81e8aec2c | CC BY 4.0 | Park benches (converted to OBJ, textures resized to 1K) |
| Roller Coaster | gaartt | https://sketchfab.com/3d-models/roller-coaster-0fe5fdbee4bf493d85b668ccaef144a5 | CC BY 4.0 | Level 4 roller coaster (bulbs split into 10 groups and the train split into 4 cars by us) |
| BOT NEIL ANIMATION | OSCAR CREATIVO (oscar_creativo) | https://sketchfab.com/3d-models/bot-neil-animation-by-oscar-creativo-56d5d623bb6c40faaa951aca02080510 | CC BY 4.0 | Codey, the robot teacher (the AI character) |

All other models (gate, paths, code stations, coins, gems, Fix Tools, extra bulbs on the Ferris wheel and carousel, lamp glow) were built by the team in Unity from simple shapes.

## Audio

All sound effects (coin, gem, tool, correct, wrong, click, RUN, power on, gate, fireworks, celebration), the three ride music loops (carousel waltz, Ferris wheel music box, roller coaster polka) and the night ambience were **made by the team with code** (`Tools/make_sounds.py`, Python + NumPy). The melodies are our own. The only third-party sound is listed below.

| Sound | Author | Source | Licence | Used for |
|---|---|---|---|---|
| Sound effects of fireworks (Diwali celebration) | Pankaj Yadav (Engyclick) | Pixabay, track 421220 | Pixabay Content License (free use, no attribution required; credited anyway) | Fireworks during the final celebration (we cut a 30 s seamless loop) |

Note: this Pixabay track is registered with YouTube Content ID. If our demo video gets a claim on YouTube, we show the Pixabay licence certificate.

Repository note: this fireworks file is **not** included in the GitHub repository, because the Pixabay licence does not allow sharing the sound as a stand-alone file. See README, section 4, to download it.

## Code, packages and AI model

| Item | Author | Licence | Used for |
|---|---|---|---|
| XR Interaction Toolkit 3.6.1 + Starter Assets + XR Device Simulator | Unity Technologies | Unity Companion License | XR rig, grab, socket, buttons, teleport, editor simulator (ready-made) |
| OpenXR, Unity OpenXR: Meta, URP, TextMeshPro, Input System, glTFast | Unity Technologies | Unity Companion License | XR runtime, rendering, text, input, model import (ready-made) |
| Llama 3.2 1B (llama3.2:1b) | Meta | Llama 3.2 Community License | Codey's AI answers, run locally (not included in the repository) |
| Ollama | Ollama | MIT | runs the model on the laptop (not included) |

All C# scripts in Assets/Scripts and Assets/Editor, the Python backend and tests, and Tools/make_sounds.py were written by the team.
