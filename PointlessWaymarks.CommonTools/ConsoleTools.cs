using System.Text;

namespace PointlessWaymarks.CommonTools;

public static class ConsoleTools
{
    /// <summary>
    ///     INSECURE - This method is a simple way to get a * obscured string from the console
    ///     but there is no attempt to truly secure the string!
    /// </summary>
    /// <param name="displayMessage"></param>
    /// <returns></returns>
    public static string GetObscuredStringFromConsole(string displayMessage)
    {
        //https://stackoverflow.com/questions/3404421/password-masking-console-application
        var pass = string.Empty;
        Console.Write(displayMessage);
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);

            // Backspace Should Not Work
            if (!char.IsControl(key.KeyChar))
            {
                pass += key.KeyChar;
                Console.Write("*");
            }
            else
            {
                if (key.Key != ConsoleKey.Backspace || pass.Length <= 0) continue;

                pass = pass[..^1];
                Console.Write("\b \b");
            }
        }
        // Stops Receiving Keys Once Enter is Pressed
        while (key.Key != ConsoleKey.Enter);

        Console.WriteLine();

        return pass;
    }

    /// <summary>
    ///     A ReadLine implementation that allows for setting the initial text value for the
    ///     user and supports some very basic editing of the line. This is based on
    ///     https://github.com/tonerdo/readline
    /// </summary>
    /// <param name="initialText"></param>
    /// <returns></returns>
    public static string ReadLine(string initialText)
    {
        var keyHandler = new KeyHandler(initialText);

        var keyInfo = Console.ReadKey(true);
        while (keyInfo.Key != ConsoleKey.Enter)
        {
            keyHandler.Handle(keyInfo);
            keyInfo = Console.ReadKey(true);
        }

        Console.WriteLine();
        return keyHandler.Text;
    }

    public static void WriteRedWrappedTextBlock(string text, int width = 80)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        WriteWrappedTextBlock(text, width);
        Console.ResetColor();
    }

    public static void WriteWrappedTextBlock(string text, int width = 80)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Console.WriteLine(text.SplitToLines(width));
    }

    /// <summary>
    ///     Used to handle key input on the console with the goal of both allowing a default/initial
    ///     value to be put on the line for the user to edit or accept and to allow for some basic
    ///     editing keys. This is based on https://github.com/tonerdo/readline
    /// </summary>
    private class KeyHandler
    {
        private readonly ConsoleWrapper _console;
        private readonly Dictionary<string, Action> _keyActions;
        private readonly StringBuilder _text;
        private int _cursorLimit;
        private int _cursorPos;
        private ConsoleKeyInfo _keyInfo;

        public KeyHandler(string initialText)
        {
            _console = new ConsoleWrapper();

            _text = new StringBuilder();
            _keyActions = new Dictionary<string, Action>
            {
                ["LeftArrow"] = MoveCursorLeft,
                ["Home"] = MoveCursorHome,
                ["End"] = MoveCursorEnd,
                ["ControlA"] = MoveCursorHome,
                ["ControlB"] = MoveCursorLeft,
                ["RightArrow"] = MoveCursorRight,
                ["ControlF"] = MoveCursorRight,
                ["ControlE"] = MoveCursorEnd,
                ["Backspace"] = Backspace,
                ["Delete"] = Delete,
                ["ControlD"] = Delete,
                ["ControlH"] = Backspace,
                ["ControlL"] = ClearLine,
                ["Escape"] = ClearLine,
                ["ControlU"] = () =>
                {
                    while (!IsStartOfLine())
                        Backspace();
                },
                ["ControlK"] = () =>
                {
                    var pos = _cursorPos;
                    MoveCursorEnd();
                    while (_cursorPos > pos)
                        Backspace();
                },
                ["ControlW"] = () =>
                {
                    while (!IsStartOfLine() && _text[_cursorPos - 1] != ' ')
                        Backspace();
                },
                ["ControlT"] = TransposeChars
            };

            WriteString(initialText);
        }

        public string Text => _text.ToString();

        private void Backspace()
        {
            if (IsStartOfLine())
                return;

            MoveCursorLeft();
            var index = _cursorPos;
            _text.Remove(index, 1);
            var replacement = _text.ToString()[index..];
            var left = _console.CursorLeft;
            var top = _console.CursorTop;
            _console.Write($"{replacement} ");
            _console.SetCursorPosition(left, top);
            _cursorLimit--;
        }

        private string BuildKeyInput()
        {
            return _keyInfo.Modifiers != ConsoleModifiers.Control && _keyInfo.Modifiers != ConsoleModifiers.Shift
                ? _keyInfo.Key.ToString()
                : _keyInfo.Modifiers + _keyInfo.Key.ToString();
        }

        private void ClearLine()
        {
            MoveCursorEnd();
            while (!IsStartOfLine())
                Backspace();
        }

        private void Delete()
        {
            if (IsEndOfLine())
                return;

            var index = _cursorPos;
            _text.Remove(index, 1);
            var replacement = _text.ToString()[index..];
            var left = _console.CursorLeft;
            var top = _console.CursorTop;
            _console.Write($"{replacement} ");
            _console.SetCursorPosition(left, top);
            _cursorLimit--;
        }

        public void Handle(ConsoleKeyInfo keyInfo)
        {
            _keyInfo = keyInfo;

            _keyActions.TryGetValue(BuildKeyInput(), out var action);
            action ??= WriteChar;
            action.Invoke();
        }

        private bool IsEndOfBuffer()
        {
            return _console.CursorLeft == _console.BufferWidth - 1;
        }

        private bool IsEndOfLine()
        {
            return _cursorPos == _cursorLimit;
        }

        private bool IsStartOfBuffer()
        {
            return _console.CursorLeft == 0;
        }

        private bool IsStartOfLine()
        {
            return _cursorPos == 0;
        }

        private void MoveCursorEnd()
        {
            while (!IsEndOfLine())
                MoveCursorRight();
        }

        private void MoveCursorHome()
        {
            while (!IsStartOfLine())
                MoveCursorLeft();
        }

        private void MoveCursorLeft()
        {
            if (IsStartOfLine())
                return;

            if (IsStartOfBuffer())
                _console.SetCursorPosition(_console.BufferWidth - 1, _console.CursorTop - 1);
            else
                _console.SetCursorPosition(_console.CursorLeft - 1, _console.CursorTop);

            _cursorPos--;
        }

        private void MoveCursorRight()
        {
            if (IsEndOfLine())
                return;

            if (IsEndOfBuffer())
                _console.SetCursorPosition(0, _console.CursorTop + 1);
            else
                _console.SetCursorPosition(_console.CursorLeft + 1, _console.CursorTop);

            _cursorPos++;
        }

        private void TransposeChars()
        {
            if (IsStartOfLine()) return;

            var firstIdx = DecrementIf(IsEndOfLine, _cursorPos - 1);
            var secondIdx = DecrementIf(IsEndOfLine, _cursorPos);

            (_text[secondIdx], _text[firstIdx]) = (_text[firstIdx], _text[secondIdx]);

            var left = IncrementIf(AlmostEndOfLine, _console.CursorLeft);
            var cursorPosition = IncrementIf(AlmostEndOfLine, _cursorPos);

            WriteNewString(_text.ToString());

            _console.SetCursorPosition(left, _console.CursorTop);
            _cursorPos = cursorPosition;

            MoveCursorRight();
            return;

            int DecrementIf(Func<bool> expression, int index)
            {
                return expression() ? index - 1 : index;
            }

            int IncrementIf(Func<bool> expression, int index)
            {
                return expression() ? index + 1 : index;
            }

            // local helper functions
            bool AlmostEndOfLine()
            {
                return _cursorLimit - _cursorPos == 1;
            }
        }

        private void WriteChar()
        {
            WriteChar(_keyInfo.KeyChar);
        }

        private void WriteChar(char c)
        {
            if (IsEndOfLine())
            {
                _text.Append(c);
                _console.Write(c.ToString());
                _cursorPos++;
            }
            else
            {
                var left = _console.CursorLeft;
                var top = _console.CursorTop;
                var str = _text.ToString()[_cursorPos..];
                _text.Insert(_cursorPos, c);
                _console.Write(c + str);
                _console.SetCursorPosition(left, top);
                MoveCursorRight();
            }

            _cursorLimit++;
        }

        private void WriteNewString(string str)
        {
            ClearLine();
            foreach (var character in str)
                WriteChar(character);
        }

        private void WriteString(string str)
        {
            foreach (var character in str)
                WriteChar(character);
        }

        private class ConsoleWrapper
        {
            public int BufferWidth => Console.BufferWidth;
            public int CursorLeft => Console.CursorLeft;
            public int CursorTop => Console.CursorTop;
            public bool PasswordMode { get; set; }

            public void SetCursorPosition(int left, int top)
            {
                if (!PasswordMode)
                    Console.SetCursorPosition(left, top);
            }

            public void Write(string value)
            {
                if (PasswordMode)
                    value = new string(default, value.Length);

                Console.Write(value);
            }
        }
    }
}