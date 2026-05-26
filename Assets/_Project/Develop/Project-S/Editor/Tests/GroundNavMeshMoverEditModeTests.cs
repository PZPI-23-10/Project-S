using NUnit.Framework;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Editor.Tests
{
    public class GroundNavMeshMoverEditModeTests
    {
        private GameObject _gameObject;

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
                Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void Configure_AppliesAgentMovementSettings()
        {
            var mover = CreateMover(out var agent);

            mover.Configure(3.5f, 1.25f, 0.45f, 1.8f, 0.1f, 14f, 500f, 0.3f, 42);

            Assert.That(agent.speed, Is.EqualTo(3.5f).Within(0.001f));
            Assert.That(agent.stoppingDistance, Is.EqualTo(1.25f).Within(0.001f));
            Assert.That(agent.radius, Is.EqualTo(0.45f).Within(0.001f));
            Assert.That(agent.height, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(agent.baseOffset, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(agent.acceleration, Is.EqualTo(14f).Within(0.001f));
            Assert.That(agent.angularSpeed, Is.EqualTo(500f).Within(0.001f));
            Assert.That(agent.avoidancePriority, Is.EqualTo(42));
            Assert.That(agent.updateRotation, Is.False);
            Assert.That(agent.autoTraverseOffMeshLink, Is.False);
        }

        [Test]
        public void TryMoveTo_WhenAgentIsNotOnNavMesh_DoesNotMoveTransformDirectly()
        {
            var mover = CreateMover(out _);
            Vector3 startPosition = _gameObject.transform.position;

            bool moved = mover.TryMoveTo(new Vector3(10f, 0f, 10f), 1f, true);

            Assert.That(moved, Is.False);
            Assert.That(_gameObject.transform.position, Is.EqualTo(startPosition));
        }

        private GroundNavMeshMover CreateMover(out NavMeshAgent agent)
        {
            _gameObject = new GameObject("GroundNavMeshMover Test");
            agent = _gameObject.AddComponent<NavMeshAgent>();
            return _gameObject.AddComponent<GroundNavMeshMover>();
        }
    }
}
