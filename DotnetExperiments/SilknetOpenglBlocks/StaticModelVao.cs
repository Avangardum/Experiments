using System.Collections.Immutable;
using Silk.NET.OpenGL;

namespace SilknetOpenglBlocks;

public sealed class StaticModelVao
{
    private readonly GL _gl;
    private readonly uint _id;
    private readonly bool _useEbo;
    private readonly uint _vertexCount;
    
    public StaticModelVao(GL gl, IReadOnlyList<float> vertices, IReadOnlyList<int> vertexAttributeSizes) :
        this(gl, vertices, vertexAttributeSizes, []) {}
    
    public StaticModelVao(GL gl, IReadOnlyList<float> vertices, IReadOnlyList<int> vertexAttributeSizes, IReadOnlyList<uint> indices)
    {
        _gl = gl;
        
        _id = _gl.GenVertexArray();
        _gl.BindVertexArray(_id);
        
        uint vboId = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vboId);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices.ToArray(), BufferUsageARB.StaticDraw);
        
        _useEbo = indices.Any();
        if (_useEbo)
        {
            uint eboId = _gl.CreateBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, eboId);
            _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices.ToArray(), BufferUsageARB.StaticDraw);
        }
        
        int vertexSize = vertexAttributeSizes.Sum();
        uint stride = (uint)vertexSize * sizeof(float);
        for (int i = 0; i < vertexAttributeSizes.Count; i++)
        {
            _gl.EnableVertexAttribArray((uint)i);
            nint offset = vertexAttributeSizes.Take(i).Sum() * sizeof(float);
            int size = vertexAttributeSizes[i];
            _gl.VertexAttribPointer((uint)i, size, VertexAttribPointerType.Float, false, stride, offset);
        }
        
        _gl.BindVertexArray(0);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        
        _vertexCount = (uint)(_useEbo ? indices.Count : vertices.Count / vertexSize);
    }
    
    public unsafe void Draw()
    {
        _gl.BindVertexArray(_id);
        if (_useEbo) _gl.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, null);
        else _gl.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
    }
}