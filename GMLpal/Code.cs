namespace GMLpal {
    abstract class Code {
        public string file, name, code;
        public bool changed = false;
        public abstract void Save();
    }
}
