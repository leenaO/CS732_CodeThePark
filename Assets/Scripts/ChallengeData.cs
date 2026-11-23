// Code the Park - written by us (content: Shahd Alqaan & Lena Alobaid).
// The 20 C++ challenges in ONE place, so the lessons are easy to read and change.
// Each challenge: Learn (Codey explains + example) -> Try (pick 1 of 3 code blocks for the blank, press RUN)
// -> Understand (correct: green + park effect, wrong: red + why it is wrong).
// The two wrong blocks are common beginner mistakes. "___" marks the blank in the code line.
[System.Serializable]
public class Challenge
{
    public int id, level;
    public string idea;          // short name of the C++ idea
    public string learn;         // Codey's explanation (simple words, real-life example)
    public string example;       // a tiny code example shown on the screen
    public string line;          // the line to fix, with ___
    public string correct;
    public string wrongA, whyA;  // wrong block + why it is wrong
    public string wrongB, whyB;
    public string hint;          // used by a Fix Tool, and as the AI fallback
    public string exampleSays;   // what the example does, in simple words
    public string task;          // what the player must do now
    public string effect;        // what happens in the park (see LevelManager)

    public Challenge(int id, int level, string idea, string learn, string example, string line,
                     string correct, string wrongA, string whyA, string wrongB, string whyB, string hint, string effect)
    {
        this.id = id; this.level = level; this.idea = idea; this.learn = learn; this.example = example; this.line = line;
        this.correct = correct; this.wrongA = wrongA; this.whyA = whyA; this.wrongB = wrongB; this.whyB = whyB;
        this.hint = hint; this.effect = effect;
    }
}

public static class ChallengeData
{
    public static readonly string[] LevelNames = { "", "The Park Gate", "The Carousel", "The Ferris Wheel", "The Roller Coaster" };
    public static readonly string[] LevelIdeas = { "", "Output: showing things on the screen", "Variables: boxes that keep values",
                                                   "Decisions: if this, then that", "Loops and functions: repeat and reuse" };

    public static readonly Challenge[] All =
    {
        // ---------------- Level 1: The Park Gate (output) ----------------
        new Challenge(1, 1, "cout - show text",
            "cout shows something on the screen, like a sign that shows words. Text must be inside \"double quotes\".",
            "cout << \"Hello\";",
            "cout << ___;",
            "\"CODE THE PARK\"",
            "CODE THE PARK", "Text needs \"double quotes\". Without them, C++ thinks these are names, not words.",
            "'CODE THE PARK'", "Single quotes are only for ONE letter, like 'A'. Use double quotes for text.",
            "Look for the block where the words are inside double quotes.",
            "gate_sign"),

        new Challenge(2, 1, "cout - show a number",
            "cout can also show numbers. Numbers are written with digits and need NO quotes.",
            "cout << 7;",
            "cout << ___;   // the year the park opened",
            "2026",
            "two thousand", "C++ does not understand numbers written in words. Use digits.",
            "20 26", "A number cannot have a space inside it. C++ sees two numbers.",
            "A number is only digits, no spaces and no words.",
            "gate_year"),

        new Challenge(3, 1, "endl - new line",
            "endl means 'end line': the next words start on a new line, like pressing Enter.",
            "cout << \"Hi\" << endl << \"Bye\";",
            "cout << \"Code\" << ___ << \"Park\";",
            "endl",
            "end", "Almost! The word is endl, with an L at the end (end line).",
            "enter", "enter is not a C++ word. C++ uses endl for a new line.",
            "It is 'end' + the first letter of 'line'.",
            "gate_bulbs"),

        new Challenge(4, 1, "// comment",
            "A comment is a note for people. C++ ignores it. A comment starts with two slashes //.",
            "// this is a note",
            "___ This line is a note for the park workers",
            "//",
            "/", "One slash means 'divide' in C++. A comment needs TWO slashes.",
            "#", "# is not a comment in C++. Comments start with //.",
            "Comments use two of the same symbol.",
            "gate_sunwheel"),

        new Challenge(5, 1, "; end of a command",
            "In C++ every command ends with a semicolon ; - like a full stop at the end of a sentence.",
            "openDoor();",
            "openGate()___",
            ";",
            ":", "That is a colon : . C++ commands end with a semicolon ; .",
            ".", "A full stop works in English, but C++ needs a semicolon ; .",
            "It looks like a dot on top of a comma.",
            "gate_open"),

        // ---------------- Level 2: The Carousel (variables) ----------------
        new Challenge(6, 2, "int - whole numbers",
            "A variable is a box with a name that keeps a value. int boxes keep WHOLE numbers, like 3 or 10.",
            "int seats = 12;",
            "int horses = ___;",
            "6",
            "six", "C++ does not understand numbers in words. Use the digit 6.",
            "6.5", "int keeps only WHOLE numbers. You cannot have half a horse!",
            "A whole number written with a digit.",
            "carousel_lights"),

        new Challenge(7, 2, "string - text",
            "A string box keeps text, like a name. Just like with cout, the text goes inside \"double quotes\".",
            "string ride = \"Big Wheel\";",
            "string name = ___;",
            "\"Star Carousel\"",
            "Star Carousel", "Text needs \"double quotes\" around it.",
            "'Star Carousel'", "Single quotes are only for ONE letter. Text needs double quotes.",
            "Same rule as the gate sign: double quotes.",
            "carousel_name"),

        new Challenge(8, 2, "double - decimal numbers",
            "A double box keeps numbers with a decimal point, like 1.5 or 9.99. C++ uses a dot, not a comma.",
            "double height = 1.2;",
            "double price = ___;",
            "2.5",
            "2,5", "C++ uses a dot . for decimals, not a comma.",
            "\"2.5\"", "With quotes it becomes TEXT, not a number. A price must be a number.",
            "A number with a dot in it, and no quotes.",
            "carousel_price"),

        new Challenge(9, 2, "+ adding",
            "+ adds numbers, like in maths. speed = speed + 2 means: take the speed and make it 2 more.",
            "score = score + 10;",
            "speed = speed ___ 2;   // turn faster",
            "+",
            "-", "- makes the speed SMALLER. We want the carousel to go faster.",
            "x", "C++ does not use x for maths. And we want to ADD, not multiply.",
            "Faster means a bigger number.",
            "carousel_slow"),

        new Challenge(10, 2, "++ add one",
            "++ is a short way to add 1. round++ is the same as round = round + 1.",
            "lap++;",
            "round___;   // one more round",
            "++",
            "+", "One + needs a second number. To add exactly one, use ++.",
            "--", "-- takes one AWAY. We want one more round.",
            "Two of the same symbol that means 'add'.",
            "carousel_on"),

        // ---------------- Level 3: The Ferris Wheel (decisions) ----------------
        new Challenge(11, 3, "bool - true or false",
            "A bool box keeps only true or false, like a light switch: ON or OFF.",
            "bool open = false;",
            "bool power = ___;   // switch the power ON",
            "true",
            "\"true\"", "With quotes it is text. A bool is just true or false, no quotes.",
            "yes", "C++ does not know yes. A bool is true or false.",
            "ON means true, without quotes.",
            "wheel_cabins"),

        new Challenge(12, 3, ">= bigger or equal",
            ">= means 'bigger than or equal to'. 3 >= 3 is true, 5 >= 3 is true, 2 >= 3 is false.",
            "if (age >= 12)",
            "if (coins ___ 3)   // 3 coins or more",
            ">=",
            "=>", "The order matters: write > first, then = .",
            "=", "One = puts a value in a box. It does not compare.",
            "Bigger sign first, then the equal sign.",
            "wheel_gate"),

        new Challenge(13, 3, "if - a decision",
            "if checks something. If it is true, the code after it runs. If it is false, it is skipped.",
            "if (raining) takeUmbrella();",
            "___ (power) start();",
            "if",
            "If", "C++ cares about capital letters. It must be if, all small.",
            "when", "when is not a C++ word. Decisions use if.",
            "A short word, all small letters.",
            "wheel_turn"),

        new Challenge(14, 3, "else - the other way",
            "else is what happens when the if is false. If it is night, blue; ELSE gold.",
            "if (hot) drink = \"water\"; else drink = \"tea\";",
            "if (night) color = \"blue\"; ___ color = \"gold\";",
            "else",
            "otherwise", "Good English, but C++ uses else.",
            "if", "A second if needs its own (check). The other way is else.",
            "The partner of if.",
            "wheel_color"),

        new Challenge(15, 3, "&& - and",
            "&& means AND: both things must be true. power && seatsReady is true only when BOTH are true.",
            "if (hasTicket && isTall) ride();",
            "if (power ___ seatsReady) go();",
            "&&",
            "||", "|| means OR: the wheel would go even if the seats are NOT ready. Not safe!",
            "+", "+ adds numbers. To check two things together, use &&.",
            "Both must be true: two & signs.",
            "wheel_on"),

        // ---------------- Level 4: The Roller Coaster (loops and functions) ----------------
        new Challenge(16, 4, "for - repeat a number of times",
            "A for loop repeats code. for (int i = 0; i < 5; i++) runs 5 times: i is 0, 1, 2, 3, 4.",
            "for (int i = 0; i < 3; i++) jump();",
            "for (int i = 0; i < ___; i++) lightOn(i);   // 5 lights",
            "5",
            "\"5\"", "With quotes it is text. The loop needs a number.",
            "five", "C++ does not understand numbers in words.",
            "How many lights? Write it as a digit.",
            "coaster_five"),

        new Challenge(17, 4, "for - where to start",
            "The first part of a for loop says where to start counting. Programmers usually start at 0.",
            "for (int i = 0; i < 10; i++)",
            "for (int i = ___; i < 10; i++) lightOn(i);   // all 10 lights",
            "0",
            "10", "If i starts at 10, then i < 10 is already false: the loop never runs!",
            "i", "i cannot start from itself. Give it a starting number.",
            "Programmers start counting from zero.",
            "coaster_all"),

        new Challenge(18, 4, "while - repeat while true",
            "while repeats again and again WHILE something is true. lap starts at 0 and the train runs while lap < 3.",
            "while (hungry) eat();",
            "while (lap ___ 3) lap++;   // run 3 laps",
            "<",
            ">", "lap starts at 0, and 0 > 3 is false, so the train would never move.",
            "=", "One = puts a value in a box. while needs a check like < .",
            "Keep going while lap is smaller than 3.",
            "coaster_run"),

        new Challenge(19, 4, "make a function",
            "A function is a group of commands with a name, like a recipe. void show() { ... } makes a recipe called show.",
            "void wave() { handUp(); handDown(); }",
            "void ___() { lightsOn(); music(); }",
            "show",
            "void", "void is a C++ word already. A function needs its own new name.",
            "1show", "A name cannot start with a number.",
            "A short name made of letters only.",
            "coaster_show"),

        new Challenge(20, 4, "call a function",
            "To use a function, write its name and (). This is called 'calling' it. Call celebrate to finish!",
            "wave();",
            "___();",
            "celebrate",
            "Celebrate", "C++ cares about capital letters. The function is called celebrate, all small.",
            "party", "There is no function called party. Look for the celebration!",
            "The name of the big party function, all small letters.",
            "celebrate"),
    };

    // what each example means + the player's task (Codey explains these step by step)
    static readonly string[,] Extra =
    {
        { "This shows Hello on the screen.", "Make the gate sign show CODE THE PARK." },
        { "This shows the number 7.", "Show the year the park opened: 2026." },
        { "This shows Hi, and then Bye on the NEXT line.", "Put Code and Park on two lines." },
        { "C++ skips this line. It is only a note for people.", "Turn the sentence into a note for the park workers." },
        { "The ; tells C++: this command is finished.", "Finish the command that opens the gate." },
        { "Now a box called seats keeps the number 12.", "The carousel has 6 horses. Put 6 in the box." },
        { "Now a box called ride keeps the text Big Wheel.", "Give the carousel its name: Star Carousel." },
        { "Now a box called height keeps 1.2 (one point two).", "A ticket costs two and a half: 2.5." },
        { "The score gets 10 more points.", "Make the carousel turn faster." },
        { "lap gets one bigger: 1 becomes 2.", "Add one more round." },
        { "The box open keeps false: the door is closed.", "Switch the wheel's power ON." },
        { "This is true when age is 12 or more.", "Check that you have 3 coins or more." },
        { "When it is raining, take the umbrella. If not, skip it.", "Start the wheel only if the power is on." },
        { "If it is hot: water. Otherwise: tea.", "Blue at night, otherwise gold." },
        { "Ride only if you have a ticket AND you are tall enough.", "Go only when the power AND the seats are ready." },
        { "jump() runs 3 times.", "Turn on 5 lights." },
        { "i counts 0, 1, 2 ... 9. That is 10 times.", "Start counting so that all 10 lights turn on." },
        { "Keep eating while you are hungry. Stop when you are not.", "Run laps while lap is smaller than 3." },
        { "Now wave() means: hand up, hand down.", "Give the light show function a name." },
        { "wave(); makes the wave happen.", "Call the function that starts the party!" },
    };

    static ChallengeData()
    {
        for (int i = 0; i < All.Length; i++) { All[i].exampleSays = Extra[i, 0]; All[i].task = Extra[i, 1]; }
    }

    // the C++ word each challenge teaches: it is shown in a different colour when Codey explains it
    static readonly string[] Keys = { "", "cout", "cout", "endl", "//", ";", "int", "string", "double", "+", "++",
                                      "bool", ">=", "if", "else", "&&", "for", "for", "while", "void", "()" };
    public static string Key(Challenge c) => Keys[c.id];

    // escape text for TextMeshPro and paint the taught C++ word yellow + bold
    public static string Highlight(string text, Challenge c)
    {
        string k = Key(c);
        string safe = text.Replace("<", "\u0001");                       // keep < safe, put it back below
        string ek = k.Replace("<", "\u0001");
        string mark = "<color=#FFD54A><b>" + ek + "</b></color>";
        if (char.IsLetter(k[0]))                                           // a word: only whole words (not "int" in "point")
            safe = System.Text.RegularExpressions.Regex.Replace(safe, @"\b" + System.Text.RegularExpressions.Regex.Escape(ek) + @"\b", mark);
        else if (k == "+")                                                 // a single + but not inside ++
            safe = System.Text.RegularExpressions.Regex.Replace(safe, @"(?<!\+)\+(?!\+)", mark);
        else safe = safe.Replace(ek, mark);
        return safe.Replace("\u0001", "<noparse><</noparse>");
    }

    // Codey's lesson, said one sentence after another (the taught word in yellow, the example coloured like code)
    public static string[] Lesson(Challenge c) => new[]
    {
        Highlight(c.learn, c),
        "Look at the example:  " + CodeColors.Color(c.example) + "   " + Highlight(c.exampleSays, c),
        "Now your turn! " + Highlight(c.task, c) + " Pick the right block, put it in the yellow slot, and press RUN.",
    };

    public static Challenge Get(int id) => (id >= 1 && id <= All.Length) ? All[id - 1] : null;
}
