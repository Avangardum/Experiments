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
    private bool _areVertexAttributeSizesSet;
    private bool _areVerticesSet;

    public Vao(GL gl)
    {
        _gl = gl;
        
        _id = _gl.GenVertexArray();
        _gl.BindVertexArray(_id);

        _vboId = _gl.GenBuffer();
        _eboId = _gl.GenBuffer();
        
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vboId);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _eboId);
    }
    
    public void SetVertexAttributeSizes(IReadOnlyList<int> sizes)
    {
        _gl.BindVertexArray(_id);
        _vertexSize = sizes.Sum();
        uint stride = (uint)_vertexSize * sizeof(float);
        for (int i = 0; i < sizes.Count; i++)
        {
            _gl.EnableVertexAttribArray((uint)i);
            nint offset = sizes.Take(i).Sum() * sizeof(float);
            int size = sizes[i];
            _gl.VertexAttribPointer((uint)i, size, VertexAttribPointerType.Float, false, stride, offset);
        }
        _areVertexAttributeSizesSet = true;
    }
    
    public void SetVertices(IReadOnlyList<float> vertices) => SetVertices(vertices.ToArray().AsSpan());
    
    public void SetVertices(ReadOnlySpan<float> vertices)
    {
        _gl.BindVertexArray(_id);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.DynamicDraw);
        _areVerticesSet = true;
    }
    
    public void SetElements(IReadOnlyList<uint> elements)
    {
        _gl.BindVertexArray(_id);
        _gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, elements.ToArray(), BufferUsageARB.DynamicDraw);
        _useEbo = elements.Any();
    }
    
    public void SetElements(uint eboId)
    {
        _gl.BindVertexArray(_id);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, eboId);
        _eboId = eboId;
        _useEbo = eboId > 0;
    }
    
    public void SetVertexCount(uint count) => _vertexCount = count;
    
    public unsafe void Draw()
    {
        if (!_areVerticesSet) throw new InvalidOperationException("Vertices are not set.");
        if (!_areVertexAttributeSizesSet) throw new InvalidOperationException("Vertex attribute sizes are not set");
        if (_vertexCount == 0) throw new InvalidOperationException("Vertex count is not set");
        
        _gl.BindVertexArray(_id);
        if (_useEbo) _gl.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, null);
        else _gl.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
    }
}