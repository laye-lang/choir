using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Choir;

public readonly struct Triple
    : IEquatable<Triple>
{
    public static bool operator ==(Triple left, Triple right) => left.Equals(right);
    public static bool operator !=(Triple left, Triple right) => !left.Equals(right);

    public static bool TryParse([NotNullWhen(true)] string? triple, [MaybeNullWhen(false)] out Triple result)
    {
        result = default;
        if (triple is null) return false;

        try
        {
            result = Parse(triple);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static Triple Parse(string triple)
    {
        string[] pieces = triple.Split('-');

        var arch = ArchKind.Unknown;
        var subArch = SubArchKind.None;
        var vendor = VendorKind.Unknown;
        var os = OSKind.Unknown;
        var environment = EnvironmentKind.Unknown;
        var objectFormat = ObjectFormatKind.Unknown;

        if (pieces.Length > 0) arch = ParseArch(pieces[0]);
        if (pieces.Length > 1) vendor = ParseVendor(pieces[1]);
        if (pieces.Length > 2) os = ParseOS(pieces[2]);
        if (pieces.Length > 3) environment = ParseEnvironment(pieces[3]);

        bool[] found = [
            arch != ArchKind.Unknown,
            vendor != VendorKind.Unknown,
            os != OSKind.Unknown,
            environment != EnvironmentKind.Unknown,
        ];

        // permute the components into their canonical position if necessary
        for (int pos = 0; pos < found.Length; pos++)
        {
            if (found[pos])
                continue; // already in the canonical position

            for (int i = 0; i < pieces.Length; i++)
            {
                if (i < found.Length && found[i])
                    continue; // don't re-parse anything already matched

                bool isValid;
                string p = pieces[i];

                switch (pos)
                {
                    default: throw new UnreachableException($"Unexpected triple component type at index {pos}.");

                    case 0:
                    {
                        arch = ParseArch(p);
                        isValid = arch != ArchKind.Unknown;
                    } break;

                    case 1:
                    {
                        vendor = ParseVendor(p);
                        isValid = vendor != VendorKind.Unknown;
                    } break;

                    case 2:
                    {
                        os = ParseOS(p);
                        isValid = os != OSKind.Unknown;
                    } break;

                    case 3:
                    {
                        environment = ParseEnvironment(p);
                        isValid = environment != EnvironmentKind.Unknown;

                        if (!isValid)
                        {
                            objectFormat = ParseObjectFormat(p);
                            isValid = objectFormat != ObjectFormatKind.Unknown;
                        }
                    } break;
                }

                if (!isValid)
                    continue; // try the next component

                if (pos < i)
                {
                    string currentPiece = "";
                    (currentPiece, pieces[i]) = (pieces[i], currentPiece);

                    for (int j = pos; currentPiece.Length != 0 && j < pieces.Length; j++)
                    {
                        while (j < found.Length && found[j])
                            j++;
                        (currentPiece, pieces[j]) = (pieces[j], currentPiece);
                    }
                }
                else if (pos > i)
                {
                    do
                    {
                        string currentPiece = "";

                        for (int j = i; j < pieces.Length;)
                        {
                            (currentPiece, pieces[j]) = (pieces[j], currentPiece);
                            if (currentPiece.Length == 0)
                                break;

                            while (++j < found.Length && found[j])
                            {
                                // do nothing else
                            }
                        }

                        if (currentPiece.Length != 0)
                            pieces = [.. pieces, currentPiece];

                        while (++i < found.Length && found[i])
                        {
                            // do nothing else
                        }
                    }
                    while (i < pos);
                }

                Debug.Assert(pos < pieces.Length && pieces[pos] == p, "Piece moved wrong.");
                found[pos] = true;
                break;
            }
        }

        if (found[0] && !found[1] && !found[2] && found[3] && pieces[1] == "none" && pieces[2].Length == 0)
        {
            (pieces[1], pieces[2]) = (pieces[2], pieces[1]);
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            if (string.IsNullOrEmpty(pieces[i]))
                pieces[i] = "unknown";
        }

        // any additional special case logic would go here.
        // at this point Arch, Vendor and OS have correct values.

        if (os == OSKind.Windows)
        {
            pieces = EnsureLength(pieces, 4);
            pieces[2] = "windows";

            if (environment == EnvironmentKind.Unknown)
            {
                if (objectFormat is ObjectFormatKind.Unknown or ObjectFormatKind.COFF)
                    pieces[3] = "msvc";
                else pieces[3] = objectFormat.ToTripleString();
            }
        }

        if (os == OSKind.Windows && environment != EnvironmentKind.Unknown)
        {
            if (objectFormat is not ObjectFormatKind.Unknown and not ObjectFormatKind.COFF)
            {
                pieces = EnsureLength(pieces, 5);
                pieces[4] = objectFormat.ToTripleString();
            }
        }

        // re-parse anything that needs it after all that movement
        if (pieces.Length > 0 && !found[0]) arch = ParseArch(pieces[0]);
        if (pieces.Length > 1 && !found[1]) vendor = ParseVendor(pieces[1]);
        if (pieces.Length > 2 && !found[2]) os = ParseOS(pieces[2]);
        if (pieces.Length > 3 && !found[3]) environment = ParseEnvironment(pieces[3]);
        if (pieces.Length > 4 && objectFormat == ObjectFormatKind.Unknown) objectFormat = ParseObjectFormat(pieces[4]);

        for (int i = 0; i < pieces.Length && i < found.Length; i++)
        {
            if (!found[i])
                pieces[i] = "unknown";
        }

        string normalForm = string.Join('-', pieces);
        return new(triple, normalForm, arch, subArch, vendor, os, environment, objectFormat);

        static string[] EnsureLength(string[] arr, int length)
        {
            if (arr.Length >= length)
                return arr;

            string[] newArr = new string[length];
            Array.Copy(arr, newArr, arr.Length);
            return newArr;
        }
    }

    public static ArchKind ParseArch(string arch) => arch switch
    {
        "x86" => ArchKind.X86,
        "amd64" => ArchKind.X86_64,
        "x86_64" => ArchKind.X86_64,
        "wasm32" => ArchKind.Wasm32,
        "wasm64" => ArchKind.Wasm64,
        _ => ArchKind.Unknown,
    };

    public static VendorKind ParseVendor(string vendor) => vendor switch
    {
        "pc" => VendorKind.PC,
        "amd" => VendorKind.AMD,
        "intel" => VendorKind.Intel,
        _ => VendorKind.Unknown,
    };

    public static OSKind ParseOS(string os) => os switch
    {
        "linux" => OSKind.Linux,
        "windows" => OSKind.Windows,
        _ => OSKind.Unknown,
    };

    public static EnvironmentKind ParseEnvironment(string env) => env switch
    {
        "gnu" => EnvironmentKind.GNU,
        "llvm" => EnvironmentKind.LLVM,
        "musl" => EnvironmentKind.Musl,
        "msvc" => EnvironmentKind.MSVC,
        _ => EnvironmentKind.Unknown,
    };

    public static ObjectFormatKind ParseObjectFormat(string format) => format switch
    {
        "coff" => ObjectFormatKind.COFF,
        "elf" => ObjectFormatKind.ELF,
        "wasm" => ObjectFormatKind.Wasm,
        _ => ObjectFormatKind.Unknown,
    };

    public readonly string Data, NormalForm;
    public readonly ArchKind Arch;
    public readonly SubArchKind SubArch;
    public readonly VendorKind Vendor;
    public readonly OSKind OS;
    public readonly EnvironmentKind Environment;
    public readonly ObjectFormatKind ObjectFormat;

    public string ArchName => GetNthComponent(0);
    public string VendorName => GetNthComponent(1);
    public string OSName => GetNthComponent(2);
    public string EnvironmentName => GetNthComponent(3);
    public string OSAndEnvironmentName => GetNthComponent(2) + "." + GetNthComponent(3);

    public bool IsArch16Bit => Arch.GetPointerBitWidth() == 16;
    public bool IsArch32Bit => Arch.GetPointerBitWidth() == 32;
    public bool IsArch64Bit => Arch.GetPointerBitWidth() == 64;

    public Triple()
    {
        Data = "";
        NormalForm = "";
    }

    private Triple(string data, string normalForm, ArchKind arch, SubArchKind subArch, VendorKind vendor,
        OSKind os, EnvironmentKind environment, ObjectFormatKind objectFormat)
    {
        Data = data;
        NormalForm = normalForm;
        Arch = arch;
        SubArch = subArch;
        Vendor = vendor;
        OS = os;
        Environment = environment;
        ObjectFormat = objectFormat;
    }

    private string GetNthComponent(int n)
    {
        string[] pieces = Data.Split('-');
        if (n >= pieces.Length) return "";
        return pieces[n];
    }

    public override string ToString() => NormalForm;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Triple other && Equals(other);
    public bool Equals(Triple other)
    {
        return Arch == other.Arch
            && SubArch == other.SubArch
            && Vendor == other.Vendor
            && OS == other.OS
            && Environment == other.Environment
            && ObjectFormat == other.ObjectFormat;
    }

    public override int GetHashCode() => HashCode.Combine(Arch, SubArch, Vendor, OS, Environment, ObjectFormat);

    public enum ArchKind
    {
        Unknown,
        X86,    // X86: i[3-9]86
        X86_64, // X86-64: amd64, x86_64
        Wasm32, // WebAssembly with 32-bit pointers
        Wasm64, // WebAssembly with 64-bit pointers
    }

    public enum SubArchKind
    {
        None,
    }

    public enum VendorKind
    {
        Unknown,
        PC,
        AMD,
        Intel,
    }

    public enum OSKind
    {
        Unknown,
        Linux,
        Windows,
    }

    public enum EnvironmentKind
    {
        Unknown,
        GNU,
        LLVM,
        Musl,
        MSVC,
    }

    public enum ObjectFormatKind
    {
        Unknown,
        COFF,
        ELF,
        Wasm,
    }
}

public static class TripleEnumExtensions
{
    public static string ToTripleString(this Triple.ArchKind kind, Triple.SubArchKind sub = Triple.SubArchKind.None) => kind switch
    {
        Triple.ArchKind.Unknown => "unknown",
        Triple.ArchKind.X86 => "x86",
        Triple.ArchKind.X86_64 => "x86_64",
        Triple.ArchKind.Wasm32 => "wasm32",
        Triple.ArchKind.Wasm64 => "wasm64",
        _ => throw new UnreachableException($"Invalid {nameof(Triple.ArchKind)} {kind}."),
    };

    public static string ToTripleString(this Triple.VendorKind kind) => kind switch
    {
        Triple.VendorKind.Unknown => "unknown",
        Triple.VendorKind.PC => "pc",
        Triple.VendorKind.AMD => "amd",
        Triple.VendorKind.Intel => "intel",
        _ => throw new UnreachableException($"Invalid {nameof(Triple.VendorKind)} {kind}."),
    };

    public static string ToTripleString(this Triple.OSKind kind) => kind switch
    {
        Triple.OSKind.Unknown => "unknown",
        Triple.OSKind.Linux => "linux",
        Triple.OSKind.Windows => "windows",
        _ => throw new UnreachableException($"Invalid {nameof(Triple.OSKind)} {kind}."),
    };

    public static string ToTripleString(this Triple.EnvironmentKind kind) => kind switch
    {
        Triple.EnvironmentKind.Unknown => "unknown",
        Triple.EnvironmentKind.GNU => "gnu",
        Triple.EnvironmentKind.LLVM => "llvm",
        Triple.EnvironmentKind.Musl => "musl",
        Triple.EnvironmentKind.MSVC => "msvc",
        _ => throw new UnreachableException($"Invalid {nameof(Triple.EnvironmentKind)} {kind}."),
    };

    public static string ToTripleString(this Triple.ObjectFormatKind kind) => kind switch
    {
        Triple.ObjectFormatKind.Unknown => "unknown",
        Triple.ObjectFormatKind.COFF => "coff",
        Triple.ObjectFormatKind.ELF => "elf",
        Triple.ObjectFormatKind.Wasm => "wasm",
        _ => throw new UnreachableException($"Invalid {nameof(Triple.ObjectFormatKind)} {kind}."),
    };

    public static int GetPointerBitWidth(this Triple.ArchKind kind) => kind switch
    {
        Triple.ArchKind.Unknown => 0,
        Triple.ArchKind.X86 or Triple.ArchKind.Wasm32 => 32,
        Triple.ArchKind.X86_64 or Triple.ArchKind.Wasm64 => 64,
        _ => throw new UnreachableException($"Invalid {nameof(Triple.ArchKind)} {kind}."),
    };
}
