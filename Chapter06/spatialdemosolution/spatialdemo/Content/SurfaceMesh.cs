using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpatialDemo.Common;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device3 = SharpDX.Direct3D11.Device3;

namespace SpatialDemo.Content
{
    class SurfaceMesh : Disposer
    {
        static Vector3 color = new Vector3(1.0f, 0.0f, 0.0f);
        Buffer _indexBuffer;
        int _indexCount;
        object _lockObject = new object();
        Buffer _modelConstantBuffer;
        ModelConstantBuffer _modelConstantBufferData;
        Matrix4x4 _transformMatrix;
        Buffer _vertexBuffer;

        void CalculateVertices(SpatialSurfaceMesh mesh,
    out VertexPositionColor[] vertices,
    out ushort[] indices,
    out Matrix4x4 finalMatrix)
        {
            var transformMatrix = Matrix4x4.Identity;
            var tryTransform = mesh.CoordinateSystem.TryGetTransformTo(CoordinateSystem);
            if (tryTransform != null)
                transformMatrix = tryTransform.Value;

            var vertexByteArray = mesh.VertexPositions.Data.ToArray();
            var vertexStride = (int)mesh.VertexPositions.Stride;
            var vertexCount = mesh.VertexPositions.ElementCount;

            var triangleIndexByteArray = mesh.TriangleIndices.Data.ToArray();
            var triangleCount = mesh.TriangleIndices.ElementCount;

            var vertexScale = mesh.VertexPositionScale;
            var scaleMatrix = Matrix4x4.CreateScale(vertexScale);
            finalMatrix = Matrix4x4.Transpose(scaleMatrix * transformMatrix);

            vertices = new VertexPositionColor[vertexCount];
            for (var i = 0; i < vertexCount; i++)
                TranslateVertices(vertices, vertexByteArray, vertexStride, color, i);

            indices = new ushort[triangleCount];

            var indexOffset = 0;
            for (var i = 0; i < triangleCount; i++)
            {
                var index = BitConverter.ToUInt16(triangleIndexByteArray, indexOffset);
                indexOffset += 2;
                indices[i] = index;
            }
        }

        void SetVertices(VertexPositionColor[] cubeVertices, ushort[] cubeIndices)
        {
            lock (_lockObject)
            {
                ReleaseDeviceDependentResources();

                _vertexBuffer = ToDispose(Buffer.Create(
                    DirectXDevice,
                    BindFlags.VertexBuffer,
                    cubeVertices));

                _indexCount = cubeIndices.Length;

                _indexBuffer = ToDispose(Buffer.Create(
                    DirectXDevice,
                    BindFlags.IndexBuffer,
                    cubeIndices));

                _modelConstantBuffer = ToDispose(Buffer.Create(
                    DirectXDevice,
                    BindFlags.ConstantBuffer,
                    ref _modelConstantBufferData));
            }
        }

        static void TranslateVertices(
            VertexPositionColor[] vertices,
            byte[] vertexByteArray,
            int vertexStride,
            Vector3 colorAsVector,
            int vertexNumber)
        {
            var vertexPositionX = BitConverter.ToSingle(vertexByteArray, vertexNumber * vertexStride + 0);
            var vertexPositionY = BitConverter.ToSingle(vertexByteArray, vertexNumber * vertexStride + 4);
            var vertexPositionZ = BitConverter.ToSingle(vertexByteArray, vertexNumber * vertexStride + 8);

            var vertexPositionColor =
                new VertexPositionColor(new Vector3(vertexPositionX, vertexPositionY, vertexPositionZ), color);

            vertices[vertexNumber] = vertexPositionColor;
        }

        public void CalculateAllVertices(SpatialSurfaceMesh mesh)
        {
            VertexPositionColor[] vertices;
            ushort[] indices;
            Matrix4x4 transformMatrix;

            CalculateVertices(mesh, out vertices, out indices, out transformMatrix);
            _transformMatrix = transformMatrix;

            SetVertices(vertices, indices);

            NeedsUpdate = true;
        }

        public void ReleaseDeviceDependentResources()
        {
            RemoveAndDispose(ref _modelConstantBuffer);
            RemoveAndDispose(ref _vertexBuffer);
            RemoveAndDispose(ref _indexBuffer);
        }

        public void Render(DeviceContext3 context)
        {
            context.VertexShader.SetConstantBuffers(0, _modelConstantBuffer);

            var stride = Utilities.SizeOf<VertexPositionColor>();
            var offset = 0;
            var bufferBinding = new VertexBufferBinding(_vertexBuffer, stride, offset);
            context.InputAssembler.SetVertexBuffers(0, bufferBinding);
            context.InputAssembler.SetIndexBuffer(
                _indexBuffer,
                Format.R16_UInt,
                0);

            context.DrawIndexedInstanced(_indexCount, 2, 0, 0, 0);
        }

        public void Update(DeviceContext3 context)
        {
            _modelConstantBufferData.model = _transformMatrix;

            if (!NeedsUpdate)
                return;

            context.UpdateSubresource(ref _modelConstantBufferData, _modelConstantBuffer);
            NeedsUpdate = false;
        }

        public SpatialCoordinateSystem CoordinateSystem { get; set; }

        public Device3 DirectXDevice { get; set; }

        public Guid Id { get; set; }

        public bool NeedsUpdate { get; set; }

        public DateTimeOffset UpdateTime { get; set; }
    }
}
