﻿using System.IO;

namespace GMLpal {
    class FragmentShader : Code {
        public FragmentShader(string f, string n, string c) {
            file = f;
            name = n;
            code = c;
        }

        public override void Save() {
            File.AppendAllText(file, "//######################_==_YOYO_SHADER_MARKER_==_######################@~" + code);
        }
    }
}