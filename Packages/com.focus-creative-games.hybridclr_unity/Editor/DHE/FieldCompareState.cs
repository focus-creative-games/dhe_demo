namespace HybridCLR.Editor.DHE
{
    public enum FieldCompareState
    {
        Equal,
        MemoryLayoutEqual, // offset not change, type change, but type layout not change
        NotEqual, // MemoryLayout or offset change
    }
}
