
namespace System {
    public struct Int32 {
        public int m_RealValue;
        public override int GetHashCode() {
            return m_RealValue;
        }
    }
}
