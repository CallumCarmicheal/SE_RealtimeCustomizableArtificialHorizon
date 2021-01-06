using Sandbox.ModAPI.Ingame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    class AHConfig {
        private Color? _textColor = null;
        private Color? _errorBorder = Color.Red;
        private Color? _horizonLine = null;
        private Color? _ladder = null;
        private Color? _ladderText = null;
        private Color? _radarAltitudeWarning = null;
        private Color? _pullUpWarning = null;
        private Color? _velocityVector = null;
        private Color? _boresight = null;
        private Color? _altimeterVSpeed = null;

        public Color TextColor => GetColor(_textColor);
        public Color ErrorBorder => GetColor(_errorBorder);
        public Color HorizonLine => GetColor(_horizonLine);
        public Color Ladder => GetColor(_ladder);
        public Color LadderText => GetColor(_ladderText);
        public Color RadarAltitudeWarning => GetColor(_radarAltitudeWarning);
        public Color PullUpWarning => GetColor(_pullUpWarning);
        public Color VelocityVector => GetColor(_velocityVector);
        public Color Boresight => GetColor(_boresight);
        public Color AltimeterVSpeed => GetColor(_altimeterVSpeed);

        public TextAndBorderColor AltimeterHeight { get; }
        public TextAndBorderColor SpeedIndicator { get; }

        public HorizonBackground[] BackgroundSections { get; }

        // ==========================

        public float VelocityResetAmount { get; set; } = 9;

        // =========================

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

            BackgroundSections = new HorizonBackground[4];
            for (int x = 0; x < 4; x++)
                BackgroundSections[x] = new HorizonBackground(this, $"GravityBackground{x}_");

            SpeedIndicator = new TextAndBorderColor(this, "SpeedIndicator");
            AltimeterHeight = new TextAndBorderColor(this, "AltimeterHeight");

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

            ini.Lambda(section, "VelocityResetAmount", () => VelocityResetAmount + "", (v) => VelocityResetAmount = (float) v.ToDouble());

            IniGetColor(ini, section, "TextColor", () => _textColor, (c) => _textColor = c);
            IniGetColor(ini, section, "TextColor", () => _textColor, (c) => _textColor = c);
            IniGetColor(ini, section, "ErrorBorder", () => _errorBorder, (c) => _errorBorder = c);
            IniGetColor(ini, section, "HorizonLine", () => _horizonLine, (c) => _horizonLine = c);
            IniGetColor(ini, section, "Ladder", () => _ladder, (c) => _ladder = c);
            IniGetColor(ini, section, "LadderText", () => _ladderText, (c) => _ladderText = c);
            IniGetColor(ini, section, "RadarAltitudeWarning", () => _radarAltitudeWarning, (c) => _radarAltitudeWarning = c);
            IniGetColor(ini, section, "PullUpWarning", () => _pullUpWarning, (c) => _pullUpWarning = c);
            IniGetColor(ini, section, "VelocityVector", () => _velocityVector, (c) => _velocityVector = c);
            IniGetColor(ini, section, "Boresight", () => _boresight, (c) => _boresight = c);

            for (int x = 0; x < 4; x++) BackgroundSections[x].Reload(ini, section);

            IniGetColor(ini, section, "AltimeterVSpeed", () => _altimeterVSpeed, (c) => _altimeterVSpeed = c);
            AltimeterHeight.Reload(ini, section);
            SpeedIndicator.Reload(ini, section);

            _Terminal.CustomData = ini.ToString();
            ParsedIni = true;
        }
        public Color GetColor(Color? valuesTextColor) {
            if (valuesTextColor == null) return _Horizon.ForegroundColor;
            return valuesTextColor.Value;
        }

        public Color GetColor(Color? valuesTextColor, float opacity) {
            if (valuesTextColor == null) return new Color(_Horizon.ForegroundColor, opacity);
            return new Color(valuesTextColor.Value, opacity);
        }

        public T ParseEnum<T>(string text, T @default) where T : struct {
            T enumValue;
            if (Enum.TryParse(text, out enumValue)) 
                return enumValue;
            return @default;
        }

        public void IniGetEnum<T>(MyIni ini, string section, string name, T @default, Func<T> Set, Action<T> Get) where T : struct {
            MyIniValue iniValue;
            if (!(iniValue = ini.Get(section, name)).IsEmpty) {
                T enumValue;
                if (Enum.TryParse(iniValue.ToString(), out enumValue)) {
                    ini.SetComment(section, name, "");
                    Get?.Invoke(enumValue);
                }
                else {
                    var values = string.Join(", ", Enum.GetValues(typeof(T)));
                    ini.SetComment(section, name, "Failed to parse enum, Available values: " + values);
                    Get?.Invoke(@default);
                }
            } else {
                var value= Set?.Invoke();

                ini.Set(section, name, value != null ? value.ToString() : @default.ToString());
                ini.SetComment(section, name, "");
            }
        }

        public void IniGetColor(MyIni ini, string section, string name, Func<Color?> Set, Action<Color?> Get) {
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
                    ini.SetComment(section, name, "");
                } else if (commaCount == 0) {
                    int value;
                    if (int.TryParse(strColor, out value)) {
                        ini.SetComment(section, name, "");
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
                        ini.SetComment(section, name, error);

                        Get?.Invoke(_Horizon.ForegroundColor);
                        return;
                    }

                    ini.SetComment(section, name, "");
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
                        ini.SetComment(section, name, error);

                        Get?.Invoke(_Horizon.ForegroundColor);
                        return;
                    }

                    ini.SetComment(section, name, "");
                    Get?.Invoke(new Color(r, g, b, 255));
                } 
                else if (commaCount == 3) {
                    unpack = strColor.Unformat("{0},{1},{2},{3}");

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
                        ini.SetComment(section, name, error);

                        Get?.Invoke(_Horizon.ForegroundColor);
                        return;
                    }

                    ini.SetComment(section, name, "");
                    Get?.Invoke(new Color(r, g, b, a));
                } else {
                    ini.SetComment(section, name, "Invalid format for color, (FG,BG)|(ALL)|(RGB,A)|(R,G,B)|(R,G,B,A)");
                    Get?.Invoke(_Horizon.ForegroundColor);
                }
            } else {
                var color = Set?.Invoke();

                if (color != null) 
                     ini.Set(section, name, color.Value.A != 255 
                         ? $"{color.Value.R},{color.Value.G},{color.Value.B},{color.Value.A}"
                         : $"{color.Value.R},{color.Value.G},{color.Value.B}");
                else ini.Set(section, name, "FG");
                ini.SetComment(section, name, "");
            }
        }

    }

    static class IniExtensions {
        internal static void Lambda(this MyIni ini, string section, string name, Func<string> Set, Action<MyIniValue> Get) {
            MyIniValue iniValue;
            if (!(iniValue = ini.Get(section, name)).IsEmpty) {
                Get?.Invoke(iniValue);
            } else ini.Set(section, name, Set?.Invoke());
        }
    }

    class HorizonBackground {
        public AHConfig Cfg { get; }
        public string Prefix { get; }

        private Color? _color { get; set; } = null;

        public Color Color => Cfg.GetColor(_color, Opacity);


        public float Opacity { get; set; } = 0.5f;

        public HorizonBackground(AHConfig cfg, string Prefix) {
            Cfg = cfg;
            this.Prefix = Prefix;
        }

        public void Reload(MyIni ini, string section) {
            Cfg.IniGetColor(ini, section, Prefix+"Color", () => _color, (c) => _color = c);
            ini.Lambda(section, Prefix + "Opacity", () => Opacity+"", (v) => Opacity = (float)v.ToDouble());
        }
    }

    class TextAndBorderColor {
        public AHConfig Cfg { get; }
        public string Prefix { get; }

        private Color? _textColor { get; set; } = null;
        public Color TextColor => Cfg.GetColor(_textColor);

        private Color? _borderColor { get; set; } = null;
        public Color BorderColor => Cfg.GetColor(_borderColor);

        public TextAndBorderColor(AHConfig cfg, string Prefix) {
            Cfg = cfg;
            this.Prefix = Prefix;
        }

        public void Reload(MyIni ini, string section) {
            Cfg.IniGetColor(ini, section, Prefix + "Text", () => _textColor, (c) => _textColor = c);
            Cfg.IniGetColor(ini, section, Prefix + "Border", () => _borderColor, (c) => _borderColor = c);
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
