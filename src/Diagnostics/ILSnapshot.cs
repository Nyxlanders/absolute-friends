using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AbsoluteFriends.Diagnostics;

internal class ILSnapshot
{
    private const int AnchorWindow = 3;

    private static readonly string[] _anchorSeparator = ["; "];

    private readonly Instruction[] _instructions;

    private readonly (OpCode OpCode, object? Operand)[] _originals;

    private readonly Dictionary<Instruction, int> _indices;

    private readonly int _variableCount;

    public ILSnapshot(IList<Instruction> instructions, int variableCount)
    {
        _instructions = [.. instructions];
        _originals = [.. _instructions.Select(instruction => (instruction.OpCode, instruction.Operand))];
        _indices = InstructionIndex.Build(_instructions);
        _variableCount = variableCount;
    }

    public int Count => _instructions.Length;

    public static ILPatchStatus Classify(List<ILPatchSite> sites, int[] recordedIndices, string[] recordedAnchors, out int shift)
    {
        shift = 0;

        if (sites.Count == 0)
        {
            return ILPatchStatus.NotFound;
        }

        if (recordedIndices == null || recordedIndices.Length == 0)
        {
            return ILPatchStatus.Untracked;
        }

        if (sites.Count != recordedIndices.Length)
        {
            return ILPatchStatus.CountMismatch;
        }

        if (recordedAnchors is { Length: > 0 } && !sites.Select(site => Normalize(site.Anchor)).SequenceEqual(recordedAnchors.Select(Normalize)))
        {
            return ILPatchStatus.AnchorMismatch;
        }

        shift = sites[0].Index - recordedIndices[0];

        if (sites.Select((site, index) => site.Index - recordedIndices[index]).Distinct().Count() > 1)
        {
            return ILPatchStatus.Rearranged;
        }

        return shift == 0 ? ILPatchStatus.Exact : ILPatchStatus.Shifted;
    }

    public bool Contains(Instruction instruction) => _indices.ContainsKey(instruction);

    public List<ILPatchSite> FindPatchSites(IList<Instruction> patched)
    {
        List<ILPatchSite> sites = [];
        int inserted = 0;

        foreach (var instruction in patched)
        {
            if (_indices.TryGetValue(instruction, out int index))
            {
                if (inserted > 0)
                {
                    sites.Add(new(index, inserted, DescribeAnchor(index)));

                    inserted = 0;
                }
                else if (IsRewritten(instruction, index))
                {
                    sites.Add(new(index, 0, DescribeAnchor(index)));
                }
            }
            else
            {
                inserted++;
            }
        }

        if (inserted > 0)
        {
            sites.Add(new(_instructions.Length, inserted, "end of method"));
        }

        return sites;
    }

    public int CountRemoved(IList<Instruction> patched) => _instructions.Length - patched.Count(_indices.ContainsKey);

    public int CountAddedVariables(int variableCount) => variableCount - _variableCount;

    private static string Normalize(string anchor) => string.Join("; ", anchor.Split(_anchorSeparator, StringSplitOptions.None).Select(NormalizeOpCode));

    private static string NormalizeOpCode(string part)
    {
        int space = part.IndexOf(' ');
        string name = space < 0 ? part : part.Substring(0, space);

        if (!name.EndsWith(".s", StringComparison.Ordinal))
        {
            return part;
        }

        name = name.Substring(0, name.Length - 2);

        return space < 0 ? name : name + part.Substring(space);
    }

    private static string Describe(OpCode opCode, object? operand, out bool named)
    {
        try
        {
            var (description, isNamed) = operand switch
            {
                TypeReference type => ($"{opCode.Name} {type.Name}", true),
                MemberReference member => ($"{opCode.Name} {member.DeclaringType?.Name}::{member.Name}", true),
                _ => (opCode.Name, false),
            };

            named = isNamed;

            return description;
        }
        catch
        {
            named = false;

            return "?";
        }
    }

    private bool IsRewritten(Instruction instruction, int index)
    {
        var (opCode, operand) = _originals[index];

        return instruction.OpCode != opCode || !Equals(instruction.Operand, operand);
    }

    private string DescribeAnchor(int index)
    {
        StringBuilder builder = new();

        foreach (var (opCode, operand) in _originals.Skip(index).Take(AnchorWindow))
        {
            if (builder.Length > 0)
            {
                builder.Append("; ");
            }

            builder.Append(Describe(opCode, operand, out bool named));

            if (named)
            {
                break;
            }
        }

        return builder.ToString();
    }
}
