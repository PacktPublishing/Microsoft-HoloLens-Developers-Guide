using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Input.Spatial;
using GazeSampleDirectX.Common;
using GazeSampleDirectX.Content;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace HolographicApp7.Common
{
    internal class HoloCursor : Disposer
    {
        private readonly DeviceResources _deviceResources;
        private GeometryShader _geometryShader;
        private Buffer _indexBuffer;
        private int _indexCount;
        private bool _initialized;
        private InputLayout _inputLayout;
        private bool _loadingComplete;
        private Buffer _modelConstantBuffer;
        private ModelConstantBuffer _modelConstantBufferData;
        private PixelShader _pixelShader;
        private bool _usingVprtShaders;
        private Buffer _vertexBuffer;
        private VertexShader _vertexShader;

        public HoloCursor(DeviceResources deviceResources)
        {
            _deviceResources = deviceResources;
            CreateDeviceDependentResourcesAsync();
        }

        public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, -1.0f);


        public void Update(SpatialPointerPose spatialPointerPose)
        {
            if (!_loadingComplete)
                return;
            var cam = spatialPointerPose.Head;

            var modelTranslation = Matrix4x4.CreateTranslation(Position);
            var modelRotation = RotateCursor(spatialPointerPose);

            var modelTransform = modelRotation*modelTranslation;

            _modelConstantBufferData.model = Matrix4x4.Transpose(modelTransform);

            var context = _deviceResources.D3DDeviceContext;
            context.UpdateSubresource(ref _modelConstantBufferData, _modelConstantBuffer);
        }

        private Matrix4x4 RotateCursor(SpatialPointerPose pose)
        {
            var facingNormal = Vector3.Normalize(-Position);

            var xAxisRotation = Vector3.Normalize(new Vector3(facingNormal.Z, 0.0f, -facingNormal.X));
            var yAxisRotation = Vector3.Normalize(Vector3.Cross(facingNormal, xAxisRotation));
            var rotationMatrix = new Matrix4x4(
                xAxisRotation.X, xAxisRotation.Y, xAxisRotation.Z, 1.0f,
                yAxisRotation.X, yAxisRotation.Y, yAxisRotation.Z, 1.0f,
                facingNormal.X, facingNormal.Y, facingNormal.Z, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f
                );

            return rotationMatrix;
        }

        public void Render()
        {
            if (!_loadingComplete)
                return;

            var context = _deviceResources.D3DDeviceContext;
            var stride = Utilities.SizeOf<VertexPositionColor>();
            var offset = 0;

            var bufferBinding = new VertexBufferBinding(_vertexBuffer, stride, offset);
            context.InputAssembler.SetVertexBuffers(0, bufferBinding);
            context.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.InputLayout = _inputLayout;

            context.VertexShader.SetShader(_vertexShader, null, 0);
            context.VertexShader.SetConstantBuffers(0, _modelConstantBuffer);
            if (!_usingVprtShaders)
            {
                context.GeometryShader.SetShader(_geometryShader, null, 0);
            }

            context.PixelShader.SetShader(_pixelShader, null, 0);
            context.DrawIndexedInstanced(_indexCount, 2, 0, 0, 0);
        }

        public void PositionHologram(SpatialPointerPose pointerPose)
        {
            if (pointerPose == null) return;

            var headPosition = pointerPose.Head.Position;
            var headDirection = pointerPose.Head.ForwardDirection;

            var distanceFromUser = 1.0f;
            Position = headPosition + distanceFromUser*headDirection;
        }

        public void ReleaseDeviceDependentResources()
        {
            _loadingComplete = false;
            _usingVprtShaders = false;

            RemoveAndDispose(ref _vertexShader);
            RemoveAndDispose(ref _inputLayout);
            RemoveAndDispose(ref _pixelShader);
            RemoveAndDispose(ref _geometryShader);
            RemoveAndDispose(ref _modelConstantBuffer);
            RemoveAndDispose(ref _vertexBuffer);
            RemoveAndDispose(ref _indexBuffer);
        }

        #region Creation

        public async void CreateDeviceDependentResourcesAsync()
        {
            ReleaseDeviceDependentResources();

            _usingVprtShaders = _deviceResources.D3DDeviceSupportsVprt;

            var vertexShaderByteCode = await BuildVertexShader();


            if (!_usingVprtShaders)
            {
                await BuildGeometryShader();
            }

            await BuildPixelShader();

            InputElement[] vertexDesc =
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0)
            };

            _inputLayout = ToDispose(new InputLayout(_deviceResources.D3DDevice, vertexShaderByteCode, vertexDesc));

            _modelConstantBuffer = ToDispose(Buffer.Create(
                _deviceResources.D3DDevice,
                BindFlags.ConstantBuffer,
                ref _modelConstantBufferData));

            BuildVertexBuffer();
            BuildIndexBuffer();

            _loadingComplete = true;
        }

        private void BuildIndexBuffer()
        {
            ushort[] cubeIndices =
            {
                0, 1, 2,
                0, 2, 3,
                0, 3, 4,
                0, 4, 1
            };

            _indexCount = cubeIndices.Length;

            _indexBuffer = ToDispose(Buffer.Create(
                _deviceResources.D3DDevice,
                BindFlags.IndexBuffer,
                cubeIndices));
        }

        private void BuildVertexBuffer()
        {
            VertexPositionColor[] cubeVertices =
            {
                new VertexPositionColor(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)),
                new VertexPositionColor(new Vector3(0.0f, 0.05f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)),
                new VertexPositionColor(new Vector3(0.05f, 0.00f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)),
                new VertexPositionColor(new Vector3(0.0f, -0.05f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)),
                new VertexPositionColor(new Vector3(-0.05f, 0.00f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f))
            };
            _vertexBuffer = ToDispose(Buffer.Create(
                _deviceResources.D3DDevice,
                BindFlags.VertexBuffer,
                cubeVertices));
        }

        private async Task<byte[]> BuildVertexShader()
        {
            var folder = Package.Current.InstalledLocation;

            var vertexShaderFileName = _usingVprtShaders
                ? "Content\\Shaders\\VPRTVertexShader.cso"
                : "Content\\Shaders\\VertexShader.cso";

            var vertexShaderByteCode =
                await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(vertexShaderFileName));

            _vertexShader = ToDispose(new VertexShader(_deviceResources.D3DDevice, vertexShaderByteCode));
            return vertexShaderByteCode;
        }

        private async Task BuildPixelShader()
        {
            var folder = Package.Current.InstalledLocation;
            var pixelShaderByteCode =
                await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\PixelShader.cso"));

            _pixelShader = ToDispose(new PixelShader(
                _deviceResources.D3DDevice,
                pixelShaderByteCode));
        }

        private async Task BuildGeometryShader()
        {
            var folder = Package.Current.InstalledLocation;

            var geometryShaderByteCode =
                await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\GeometryShader.cso"));

            _geometryShader = ToDispose(new GeometryShader(
                _deviceResources.D3DDevice,
                geometryShaderByteCode));
        }

        #endregion
    }
}