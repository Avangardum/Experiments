using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace SilknetOpenglBlocks;

public class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private bool _isDisposed;

    public uint Id { get; }
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
        Id = _gl.CreateProgram();
        shaderIds.ForEach(x => _gl.AttachShader(Id, x));
        _gl.LinkProgram(Id);
        _gl.GetProgram(Id, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
            throw new Exception($"{name} failed to link.\n{_gl.GetProgramInfoLog(Id)}");
        shaderIds.ForEach(x => _gl.DetachShader(Id, x));
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
    
    public void Use()
    {
        if (_isDisposed) throw new ObjectDisposedException($"Shader \"{Name}\"");
        _gl.UseProgram(Id);
    }

    public void Dispose()
    {
        _isDisposed = true;
        _gl.DeleteProgram(Id);
        GC.SuppressFinalize(this);
    }
    
    ~ShaderProgram()
    {
        Dispose();
    }
    
    private int GetUniformLocation(string name)
    {
        int location = _gl.GetUniformLocation(Id, name);
        if (location == -1) throw new ArgumentException($"Uniform {name} not found.");
        return location;
    }

    public void SetUniform(string name, float value)
    {
        _gl.Uniform1(GetUniformLocation(name), value);
    }
    
    public unsafe void SetUniform(string name, Matrix4x4 value)
    {
        _gl.UniformMatrix4(GetUniformLocation(name), 1, false, (float*)&value);
    }
    
    public unsafe void SetUniform(string name, Matrix4X4<float> value)
    {
        _gl.UniformMatrix4(GetUniformLocation(name), 1, false, (float*)&value);
    }
}