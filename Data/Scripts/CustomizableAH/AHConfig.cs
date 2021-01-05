using Sandbox.ModAPI.Ingame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using EmptyKeys.UserInterface.Generated.StoreBlockView_Bindings;
using Gwindalmir;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;


namespace CustomizableAH {

    class AHConfigValues {
   
    }

    class AHConfig {
        private Color? _textColor = null;
        private Color? _errorBorderColor = Color.Red;
        private Color? _gridBackgroundColor = null;

        public Color HorizonLine => GetColor(_gridBackgroundColor);
        public Color GridBackgroundColor => GetColor(_gridBackgroundColor);
        public Color TextColor => GetColor(_textColor);
        public Color ErrorBorderColor => GetColor(_errorBorderColor);

        // ==========================

        public float DebugFloat1 { get; set; } = 10;
        public float GridBackgroundOpacity { get; set; } = 0.5f;

        public bool PauseOnNoPhysics = true;

        // =========================

        protected AHConfigValues values;

        public TSSArtificialHorizon _Horizon { get; set; }
        public IMyTextSurface _Surface { get; }
        public IMyCubeBlock _Block { get; }
        public IMyTerminalBlock _Terminal { get; }
        public Vector2 _Size { get; }

        public bool   ParsedIni = false;

        public AHConfig(TSSArtificialHorizon horizon, IMyTextSurface surface, IMyCubeBlock block, Vector2 size) {
            _Horizon = horizon;
            _Surface = surface;
            _Block = block;
            _Size = size;
            _Terminal = block as IMyTerminalBlock;

            ReloadValues();
        }


        public void ReloadValues() {
            if (_Terminal == null) return;

            var section = $"CustomizableAH ({_Surface.DisplayName})";

            MyIni ini = new MyIni();
            MyIniParseResult result;
            if (!ini.TryParse(_Terminal.CustomData, out result)) {
                ParsedIni = false;
                return;
            }

            ini.Set(section, "LastTick", DateTime.Now + "");
            ini.Lambda(section, "DebugFloat1", () => DebugFloat1 + "", (v) => DebugFloat1 = (float) v.ToDouble());
            ini.Lambda(section, "PauseOnNoPhysics", () => PauseOnNoPhysics + "", (v) => PauseOnNoPhysics = v.ToBoolean());

            IniGetColor(ini, section, "TextColor", () => _textColor, (c) => _textColor = c);
            IniGetColor(ini, section, "ErrorBorderColor", () => _errorBorderColor, (c) => _errorBorderColor = c);
            IniGetColor(ini, section, "GridBackgroundColor", () => _gridBackgroundColor, (c) => _gridBackgroundColor = c);
            ini.Lambda(section, "GridBackgroundOpacity", () => GridBackgroundOpacity + "", (v) => GridBackgroundOpacity = (float)v.ToDouble());

            _Terminal.CustomData = ini.ToString();
            ParsedIni = true;
        }
        private Color GetColor(Color? valuesTextColor) {
            if (valuesTextColor == null) return _Horizon.ForegroundColor;
            return valuesTextColor.Value;
        }

        void IniGetColor(MyIni ini, string section, string name, Func<Color?> Set, Action<Color?> Get) {
            MyIniValue iniValue;
            string strColor;

            if (!(iniValue = ini.Get(section, name)).IsEmpty) {
                strColor = iniValue.ToString();
                string[] unpack = null;

                int commaCount = strColor.Count(f => (f == ','));

                if (strColor == "FG" || strColor == "BG") {
                    switch (strColor) {
                    case "BG":
                        Get?.Invoke(_Horizon.BackgroundColor);
                        break;
                    default:
                        Get?.Invoke(_Horizon.ForegroundColor);
                        break;
                    } 
                }

                else if (commaCount == 0) {
                    int value;
                    if (int.TryParse(strColor, out value)) {
                        Get?.Invoke(new Color(value, value, value, 255));
                    }
                    else {
                        ini.SetComment(section, name, "Failed to parse as single integer");
                    }
                }

                else if (commaCount == 1) {
                    unpack = strColor.Unformat("{0},{1}");

                    int rgb = 0, a = 0;

                    bool tryRgb = int.TryParse(unpack[0], out rgb);
                    bool tryA   = int.TryParse(unpack[1], out rgb);

                    if (!tryRgb || !tryA) {
                        var error = "Error: " + strColor;
                        if (!tryRgb) error += ", RGB not valid int";
                        if (!tryA)   error += ", Alpha not valid int";
                        ini.SetComment(section, name, "Failed to parse as single integer");

                        Get?.Invoke(_Horizon.ForegroundColor);
                        return;
                    }

                    Get?.Invoke(new Color(rgb, rgb, rgb, a));
                }

                else if (commaCount == 2) {
                    unpack = strColor.Unformat("{0},{1},{2}");

                    int r = 0, g = 0, b = 0, a = 255;

                    bool tryR = int.TryParse(unpack[0], out r);
                    bool tryG = int.TryParse(unpack[1], out g);
                    bool tryB = int.TryParse(unpack[2], out b);

                    if (!tryR || !tryG || !tryB) {
                        var error = "Error: " + strColor;
                        if (!tryR) error += ", Red not valid int";
                        if (!tryG) error += ", Green not valid int";
                        if (!tryB) error += ", Blue not valid int";
                        ini.SetComment(section, name, "Failed to parse as single integer");

                        Get?.Invoke(_Horizon.ForegroundColor);
                        return;
                    }

                    Get?.Invoke(new Color(r, g, b, 255));
                } 
                else if (commaCount == 3) {
                    unpack = strColor.Unformat("{0},{1},{2}");

                    int r = 0, g = 0, b = 0, a = 0;

                    bool tryR = int.TryParse(unpack[0], out r);
                    bool tryG = int.TryParse(unpack[1], out g);
                    bool tryB = int.TryParse(unpack[2], out b);
                    bool tryA = int.TryParse(unpack[2], out a);


                    if (!tryR || !tryG || !tryB || !tryA) {
                        var error = "Error: " + strColor;
                        if (!tryR) error += ", Red not valid int";
                        if (!tryG) error += ", Green not valid int";
                        if (!tryB) error += ", Blue not valid int";
                        if (!tryA) error += ", Alpha not valid int";
                        ini.SetComment(section, name, "Failed to parse as single integer");

                        Get?.Invoke(_Horizon.ForegroundColor);
                        return;
                    }

                    Get?.Invoke(new Color(r, g, b, 255));
                } else {
                    ini.SetComment(section, name, "Invalid format for color, (FG,BG)|(ALL)|(RGB,A)|(R,G,B)|(R,G,B,A)");
                    Get?.Invoke(_Horizon.ForegroundColor);
                }
            } else {
                Color? color = Set?.Invoke();

                if (color != null) 
                    ini.Set(section, name, color.ToString());
            }
        }

    }

    static class IniExtensions {
        internal static void Lambda(this MyIni ini, string title, string name, Func<string> Set, Action<MyIniValue> Get) {
            MyIniValue iniValue;
            if (!(iniValue = ini.Get(title, name)).IsEmpty) {
                Get?.Invoke(iniValue);
            } else ini.Set(title, name, Set?.Invoke());
        }
    }

    public static class StringExtensions {
        #region Cached Compiled Regular Expressions

        private static object _initLock = new object();

        private static Regex _escapeRegEx;
        private static Regex EscapeRegEx {
            get {
                if (_escapeRegEx == null)
                    lock (_initLock)
                        if (_escapeRegEx == null)
                            InitExpressions();

                return _escapeRegEx;
            }
        }

        private static Regex _selectorRegEx;
        private static Regex SelectorRegEx {
            get {
                if (_selectorRegEx == null)
                    lock (_initLock)
                        if (_selectorRegEx == null)
                            InitExpressions();

                return _selectorRegEx;
            }
        }

        private static void InitExpressions() {
            // init the lot at once: we're going to need them all
            _escapeRegEx = new Regex(@"([\[\^\$\.\|\?\*\+\(\)])", RegexOptions.Compiled);
            _selectorRegEx = new Regex(@"\{([0-9]+)\}");
        }

        #endregion

        public static string[] Unformat(this String input, string formatString) {
            // Escape special regular expression characters with a backslash
            string interim = EscapeRegEx.Replace(formatString, @"\$1");

            // Turn format string style {0} into regex style (?<C0>.+)
            // Note that this doesn't support better formatting yet: e.g. {0:d}
            interim = SelectorRegEx.Replace(interim, @"(?<C$1>.+)");

            // add start and end markers
            interim = String.Format(@"^{0}$", interim);

            // perform the match
            Regex regex = new Regex(interim);
            Match match = regex.Match(input);

            // loop from zero until we don't get a matched capture group
            List<string> output = new List<string>();
            int loop = 0;
            while (true) {
                // build a capture group name and check for it
                string captureName = String.Format("C{0}", loop++);
                Group capture = match.Groups[captureName];

                //  see if this capture was found
                if (capture == null || !capture.Success)
                    break;

                // add it to the output list
                output.Add(capture.Value);
            }

            return output.ToArray();
        }
    }
}
