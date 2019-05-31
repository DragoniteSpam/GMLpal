using System.IO;

namespace GMLpal {
    class Script: Code {
        public Script(string f, string n, string c) {
            file = f;
            name = n;
            code = c;
        }

        public override void Save() {
            if (!changed) return;
            changed = false;
            File.WriteAllText(file, code);
        }
    }
}
