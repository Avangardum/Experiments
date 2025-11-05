using System.Collections.Immutable;
using Silk.NET.OpenGL;

namespace SilknetOpenglBlocks;

public sealed class StaticModelVao
{
    private readonly GL _gl;
    private readonly uint _id;
    private readonly bool _useEbo;
    private readonly uint _vertexCount;
    
    public StaticModelVao(GL gl, ReadOnlySpan<float> vertices, ReadOnlySpan<int> vertexAttributeSizes) :
        this(gl, vertices, vertexAttributeSizes, []) {}
    
    public StaticModelVao(GL gl, ReadOnlySpan<float> vertices, ReadOnlySpan<int> vertexAttributeSizes, ReadOnlySpan<uint> indices)
    {
        _gl = gl;
        
        _id = _gl.GenVertexArray();
        _gl.BindVertexArray(_id);
        
        uint vboId = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vboId);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);
        
        _useEbo = !indices.IsEmpty;
        if (_useEbo)
        {
            uint eboId = _gl.CreateBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, eboId);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);
        }
        
        int vertexSize = vertexAttributeSizes.ToImmutableArray().Sum();
        uint stride = (uint)vertexSize * sizeof(float);
        for (int i = 0; i < vertexAttributeSizes.Length; i++)
        {
            _gl.EnableVertexAttribArray((uint)i);
            nint offset = vertexAttributeSizes.ToImmutableArray().Take(i).Sum();
            int size = vertexAttributeSizes[i];
            _gl.VertexAttribPointer((uint)i, size, VertexAttribPointerType.Float, false, stride, offset);
        }
        
        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        
        _vertexCount = (uint)(_useEbo ? indices.Length : vertices.Length / vertexSize);
    }
    
    public unsafe void Draw(ShaderProgram program)
    {
        program.Use();
        _gl.BindVertexArray(_id);
        
        if (_useEbo) _gl.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, (void*)0);
        else _gl.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
        
        _gl.UseProgram(0);
        _gl.BindVertexArray(0);
    }
}