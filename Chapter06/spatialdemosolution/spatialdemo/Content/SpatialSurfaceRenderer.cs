using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpatialDemo.Common;
using System;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel;
using Windows.Graphics.DirectX;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;

namespace SpatialDemo.Content
{
    class SpatialSurfaceRenderer : Disposer
    {
        DeviceResources _deviceResources;
        GeometryShader _geometryShader;
        InputLayout _inputLayout;
        bool _loadingComplete;
        PixelShader _pixelShader;
        SpatialCoordinateSystem _spatialCoordinateSystem;
        SpatialSurfaceMeshOptions _spatialSurfaceMeshOptions;
        SpatialSurfaceObserver _spatialSurfaceObserver;
        RasterizerState _state;
        SurfaceMeshList _surfaceMeshList = new SurfaceMeshList();
        bool _usingVprtShaders;
        VertexShader _vertexShader;

        public SpatialSurfaceRenderer(DeviceResources deviceResources, SpatialCoordinateSystem spatialCoordinateSystem)
        {
            _deviceResources = deviceResources;
            _spatialCoordinateSystem = spatialCoordinateSystem;
            CheckAccess();

            var desc = RasterizerStateDescription.Default();
            desc.FillMode = FillMode.Wireframe;

            _state = new RasterizerState(deviceResources.D3DDevice, desc);

            SurfaceMeshList.CoordinateSystem = _spatialCoordinateSystem;
            _surfaceMeshList.DirectXDevice = deviceResources.D3DDevice;

            CreateDeviceDependentResourcesAsync();
        }

        void BuildSpatialSurfaceObserver()
        {
            _spatialSurfaceObserver = new SpatialSurfaceObserver();

            var positionFormat = DirectXPixelFormat.R32G32B32A32Float;
            var normalFormat = DirectXPixelFormat.R32G32B32A32Float;

            _spatialSurfaceMeshOptions = new SpatialSurfaceMeshOptions
            {
                IncludeVertexNormals = true,
                VertexPositionFormat = positionFormat,
                VertexNormalFormat = normalFormat,
                TriangleIndexFormat = DirectXPixelFormat.R16UInt
            };

            var boundingBox = new SpatialBoundingBox
            {
                Center = new Vector3(0f, 0f, 0f),
                Extents = new Vector3(10f, 10f, 10f)
            };
            var bounds = SpatialBoundingVolume.FromBox(_spatialCoordinateSystem, boundingBox);

            _spatialSurfaceObserver.SetBoundingVolume(bounds);

            _spatialSurfaceObserver.ObservedSurfacesChanged += SpatialSurfaceObserverOnObservedSurfacesChanged;
        }

        async void CheckAccess()
        {
            var res = await SpatialSurfaceObserver.RequestAccessAsync();

            if (res != SpatialPerceptionAccessStatus.Allowed)
                throw new Exception("No access to spatial data.");
        }

        async void SpatialSurfaceObserverOnObservedSurfacesChanged(SpatialSurfaceObserver sender, object args)
        {
            var observedSurfaces = _spatialSurfaceObserver.GetObservedSurfaces();

            foreach (var surfacePair in observedSurfaces)
            {
                var spatialSurfaceInfo = surfacePair.Value;
                await _surfaceMeshList.AddOrUpdateAsync(spatialSurfaceInfo, _spatialSurfaceMeshOptions);
            }

            var allIds = (from item in observedSurfaces
                          select item.Key).ToList();
            _surfaceMeshList.Prune(allIds);
            _loadingComplete = true;
        }

        public async void CreateDeviceDependentResourcesAsync()
        {
            ReleaseDeviceDependentResources();

            _usingVprtShaders = _deviceResources.D3DDeviceSupportsVprt;

            var folder = Package.Current.InstalledLocation;

            var vertexShaderFileName = _usingVprtShaders
                ? "Content\\Shaders\\VPRTVertexShader.cso"
                : "Content\\Shaders\\VertexShader.cso";

            var vertexShaderByteCode =
                await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(vertexShaderFileName));

            _vertexShader = ToDispose(new VertexShader(
                _deviceResources.D3DDevice,
                vertexShaderByteCode));

            var vertexDesc = new InputElement[]            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0)
    };

            _inputLayout = ToDispose(new InputLayout(
                _deviceResources.D3DDevice,
                vertexShaderByteCode,
                vertexDesc));

            if (!_usingVprtShaders)
            {
                var geometryShaderByteCode =
                    await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\GeometryShader.cso"));

                _geometryShader = ToDispose(new GeometryShader(_deviceResources.D3DDevice, geometryShaderByteCode));
            }

            var pixelShaderByteCode =
                await DirectXHelper.ReadDataAsync(await folder.GetFileAsync("Content\\Shaders\\PixelShader.cso"));

            _pixelShader = ToDispose(new PixelShader(
                _deviceResources.D3DDevice,
                pixelShaderByteCode));

            BuildSpatialSurfaceObserver();
        }

public void ReleaseDeviceDependentResources()
{
    _loadingComplete = false;
    _usingVprtShaders = false;
    RemoveAndDispose(ref _pixelShader);
    RemoveAndDispose(ref _vertexShader);
    RemoveAndDispose(ref _geometryShader);
    RemoveAndDispose(ref _inputLayout);

    _surfaceMeshList.ReleaseDeviceDependentResources();
}

        public void Render()
        {
            // Loading is asynchronous. Resources must be created before drawing can occur.
            if (!_loadingComplete)
                return;

            var context = _deviceResources.D3DDeviceContext;

            // Attach the vertex shader.
            context.VertexShader.SetShader(_vertexShader, null, 0);

            if (!_usingVprtShaders)
                context.GeometryShader.SetShader(_geometryShader, null, 0);

            // Attach the pixel shader.
            context.PixelShader.SetShader(_pixelShader, null, 0);

            context.InputAssembler.InputLayout = _inputLayout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.Rasterizer.State = _state;

            var allMeshes = _surfaceMeshList.Meshes();
            foreach (var mesh in allMeshes)
                mesh.Render(context);
        }

        public void Update()
        {
            var allMeshes = _surfaceMeshList.GetAllUpdatedMeshes();
            foreach (var mesh in allMeshes)
                mesh.Update(_deviceResources.D3DDeviceContext);
        }
    }
}