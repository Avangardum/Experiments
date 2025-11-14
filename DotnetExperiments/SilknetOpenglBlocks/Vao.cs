using System.Collections.Immutable;
using Silk.NET.OpenGL;

namespace SilknetOpenglBlocks;

public sealed class Vao
{
    private readonly GL _gl;
    private readonly uint _id;
    private uint _vertexCount;
    private uint _vboId;
    private uint _eboId;
    private int _vertexSize;
    private bool _useEbo;

    public Vao(GL gl, IReadOnlyList<float> vertices, IReadOnlyList<int> vertexAttributeSizes, IReadOnlyList<uint> indices)
    {
        _gl = gl;
        
        _id = _gl.GenVertexArray();
        _gl.BindVertexArray(_id);

        _vboId = _gl.GenBuffer();
        _eboId = _gl.CreateBuffer();
        
        Update(vertices, indices);
        
        _vertexSize = vertexAttributeSizes.Sum();
        uint stride = (uint)_vertexSize * sizeof(float);
        for (int i = 0; i < vertexAttributeSizes.Count; i++)
        {
            _gl.EnableVertexAttribArray((uint)i);
            nint offset = vertexAttributeSizes.Take(i).Sum() * sizeof(float);
            int size = vertexAttributeSizes[i];
            _gl.VertexAttribPointer((uint)i, size, VertexAttribPointerType.Float, false, stride, offset);
        }
    }
    
    public void Update(IReadOnlyList<float> vertices, IReadOnlyList<uint> indices)
    {
        _gl.BindVertexArray(_id);
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboId);
        _gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices.ToArray(), BufferUsageARB.DynamicDraw);
        
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _eboId);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices.ToArray(), BufferUsageARB.DynamicDraw);
        _useEbo = indices.Any();
        
        _vertexCount = (uint)(_useEbo ? indices.Count : vertices.Count / _vertexSize);
    }
    
    public unsafe void Draw()
    {
        _gl.BindVertexArray(_id);
        if (_useEbo) _gl.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, null);
        else _gl.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
    }
}