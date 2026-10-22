using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;

namespace AbsoluteFriends.Diagnostics;

internal static class InstructionIndex
{
    public static Dictionary<Instruction, int> Build(IList<Instruction> instructions)
    {
        Dictionary<Instruction, int> indices = new(instructions.Count, ReferenceComparer.Instance);

        for (int i = 0; i < instructions.Count; ++i)
        {
            indices[instructions[i]] = i;
        }

        return indices;
    }

    private class ReferenceComparer : IEqualityComparer<Instruction>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(Instruction? x, Instruction? y) => ReferenceEquals(x, y);

        public int GetHashCode(Instruction obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
