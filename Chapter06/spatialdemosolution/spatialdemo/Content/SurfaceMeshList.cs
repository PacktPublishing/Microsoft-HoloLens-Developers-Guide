using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;

namespace SpatialDemo.Content
{
    class SurfaceMeshList
    {
        Dictionary<Guid, SurfaceMesh> _internalList = new Dictionary<Guid, SurfaceMesh>();
        public object _lockObject = new object();

        public async Task AddOrUpdateAsync(SpatialSurfaceInfo spatialSurfaceInfo, SpatialSurfaceMeshOptions options)
        {
            var mesh = await spatialSurfaceInfo.TryComputeLatestMeshAsync(1000.0d, options);

            if (mesh == null)
                return;

            var tempList = new Dictionary<Guid, SurfaceMesh>(_internalList);

            // See if we already have this one....
            var hasOne = tempList.ContainsKey(mesh.SurfaceInfo.Id);

            if (hasOne)
            {
                // Update
                var surfaceMesh = tempList[mesh.SurfaceInfo.Id];
                if (surfaceMesh.UpdateTime < mesh.SurfaceInfo.UpdateTime)
                {
                    surfaceMesh.CalculateAllVertices(mesh);
                    surfaceMesh.UpdateTime = mesh.SurfaceInfo.UpdateTime;
                }
            }
            else
            {
                // Add new
                var newMesh = new SurfaceMesh
                {
                    Id = mesh.SurfaceInfo.Id,
                    UpdateTime = mesh.SurfaceInfo.UpdateTime,
                    CoordinateSystem = CoordinateSystem,
                    DirectXDevice = DirectXDevice
                };

                newMesh.CalculateAllVertices(mesh);
                tempList[newMesh.Id] = newMesh;
            }

            lock (_lockObject)
                _internalList = tempList;
        }

        public IList<SurfaceMesh> GetAllUpdatedMeshes() => (from item in _internalList.Values
                                                            where item.NeedsUpdate
                                                            select item).ToList();

        public IEnumerable<SurfaceMesh> Meshes() => _internalList.Values.ToList();

        public void Prune(List<Guid> allIds)
        {
            var removableIds = new List<Guid>();

            var tempList = new Dictionary<Guid, SurfaceMesh>(_internalList);

            foreach (var id in tempList.Keys)
                if (!allIds.Contains(id))
                    removableIds.Add(id);

            for (var i = 0; i < removableIds.Count; i++)
            {
                tempList[removableIds[i]].ReleaseDeviceDependentResources();
                tempList.Remove(removableIds[i]);
            }

            lock (_lockObject)
                _internalList = tempList;
        }

        public void ReleaseDeviceDependentResources()
        {
            foreach (var surfaceMesh in _internalList.Values)
                surfaceMesh.ReleaseDeviceDependentResources();

            _internalList.Clear();
        }

        public static SpatialCoordinateSystem CoordinateSystem { get; set; }

        public Device3 DirectXDevice { get; set; }
    }
}
