using System.Collections.Immutable;
using Silk.NET.OpenGL;

namespace SilknetOpenglBlocks;

public sealed class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly uint _id;
    private bool _isDisposed;
    
    public string Name { get; }
    
    private static readonly ImmutableList<(ShaderType ShaderType, string Extension)> ShaderTypesAndExtensions =
        [
            (ShaderType.VertexShader, ".vert"),
            (ShaderType.FragmentShader, ".frag")
        ];
    
    public ShaderProgram(GL gl, string name)
    {
        _gl = gl;
        Name = name;
        
        ImmutableList<uint> shaderIds = LoadAndCompileShaders();
        
        _id = _gl.CreateProgram();
        shaderIds.ForEach(x => _gl.AttachShader(_id, x));
        _gl.LinkProgram(_id);
        _gl.GetProgram(_id, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
            throw new Exception($"{name} failed to link.\n{_gl.GetProgramInfoLog(_id)}");
        shaderIds.ForEach(x => _gl.DetachShader(_id, x));
        shaderIds.ForEach(x => _gl.DeleteShader(x));
    }
    
    private ImmutableList<uint> LoadAndCompileShaders()
    {
        ImmutableList<uint> shaderIds = ShaderTypesAndExtensions
            .Select(uint? (x) =>
            {
                string sourceFileName = Name + x.Extension;
                string sourceFilePath = Path.Combine("Shaders", Name, sourceFileName);
                if (!File.Exists(sourceFilePath)) return null;
                string source = File.ReadAllText(sourceFilePath);
                uint shaderId = _gl.CreateShader(x.ShaderType);
                _gl.ShaderSource(shaderId, source);
                _gl.CompileShader(shaderId);
                _gl.GetShader(shaderId, ShaderParameterName.CompileStatus, out int compileStatus);
                if (compileStatus == 0)
                    throw new Exception($"{sourceFileName} failed to compile.\n{_gl.GetShaderInfoLog(shaderId)}");
                return shaderId;
            })
            .OfType<uint>()
            .ToImmutableList();
        if (!shaderIds.Any()) throw new Exception($"No shaders found for program {Name}.");
        return shaderIds;
    }
    
    public void Use() => _gl.UseProgram(_id);
    
    public void Dispose()
    {
        _gl.DeleteProgram(_id);
        GC.SuppressFinalize(this);
    }
    
    ~ShaderProgram()
    {
        Dispose();
    }
}