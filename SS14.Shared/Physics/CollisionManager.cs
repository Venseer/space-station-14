﻿using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Physics
{
    //Its the bucket list!
    /// <summary>
    ///     Here's what is happening here. Each collidable AABB added to this manager gets tossed into
    ///     a "bucket". The buckets are subdivisions of the world space in 256-unit blocks.
    /// </summary>
    public class CollisionManager : ICollisionManager
    {
        private const int BucketSize = 256;
        private readonly Dictionary<ICollidable, (CollidableAABB aabb, IEntity owner)> _aabbs;

        private readonly Dictionary<Vector2i, int> _bucketIndex;
        //Indexed in 256-pixel blocks - 0 = 0, 1 = 256, 2 = 512 etc

        private readonly Dictionary<int, CollidableBucket> _buckets;
        // each bucket represents a 256x256 block of pixelspace

        private int _lastIndex;

        /// <summary>
        ///     Constructor
        /// </summary>
        public CollisionManager()
        {
            _bucketIndex = new Dictionary<Vector2i, int>();
            _buckets = new Dictionary<int, CollidableBucket>();
            _aabbs = new Dictionary<ICollidable, (CollidableAABB aabb, IEntity owner)>();
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management. Does not trigger Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool IsColliding(Box2 collider, MapId map)
        {
            Vector2[] points =
            {
                new Vector2(collider.Left, collider.Top),
                new Vector2(collider.Right, collider.Top),
                new Vector2(collider.Right, collider.Bottom),
                new Vector2(collider.Left, collider.Bottom)
            };

            var colliders = points.Select(GetBucket) // Get the buckets that correspond to the collider's points.
                        .Distinct()
                        .SelectMany(b => b.GetPoints()) // Get all of the points
                        .Select(p => p.ParentAABB) // Expand points to distinct AABBs
                        .Distinct();

            foreach (var aabb in colliders)
            {
                if (aabb.Collidable.MapID == map
                    && aabb.Collidable.WorldAABB.Intersects(collider)
                    && aabb.Collidable.IsHardCollidable)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool TryCollide(IEntity collider)
        {
            return TryCollide(collider, new Vector2());
        }

        /// <summary>
        ///     returns true if collider intersects a collidable under management and calls Bump.
        /// </summary>
        /// <param name="collider">Rectangle to check for collision</param>
        /// <returns></returns>
        public bool TryCollide(IEntity collider, Vector2 offset, bool bump = true)
        {
            if (collider == null) return false;
            var colliderComponent = collider.GetComponent<ICollidableComponent>();
            if (colliderComponent == null) return false;

            var colliderAABB = colliderComponent.WorldAABB;
            if (offset.LengthSquared > 0)
            {
                colliderAABB = colliderAABB.Translated(offset);
            }

            Vector2[] points =
            {
                new Vector2(colliderAABB.Left, colliderAABB.Top),
                new Vector2(colliderAABB.Right, colliderAABB.Top),
                new Vector2(colliderAABB.Right, colliderAABB.Bottom),
                new Vector2(colliderAABB.Left, colliderAABB.Bottom)
            };

            var bounds = points
                    .Select(GetBucket) // Get the buckets that correspond to the collider's points.
                    .Distinct()
                    .SelectMany(b => b.GetPoints()) // Get all of the points
                    .Select(p => p.ParentAABB) // Expand points to distinct AABBs
                    .Distinct()
                    .Where(aabb => aabb.Collidable != colliderComponent &&
                           aabb.Collidable.WorldAABB.Intersects(colliderAABB) &&
                           aabb.Collidable.MapID == colliderComponent.MapID); //try all of the AABBs against the target rect.

            //See if our collision will be overriden by a component
            List<ICollideSpecial> collisionmodifiers = collider.GetAllComponents<ICollideSpecial>().ToList();
            List<IEntity> collidedwith = new List<IEntity>();

            //try all of the AABBs against the target rect.
            var collided = false;
            foreach (var aabb in bounds)
            {
                //Provides component level overrides for collision behavior based on the entity we are trying to collide with
                var preventcollision = false;
                for (var i = 0; i < collisionmodifiers.Count; i++)
                {
                    preventcollision |= collisionmodifiers[i].PreventCollide(aabb.Collidable);
                }
                if (preventcollision) //We were prevented, bail
                    continue;

                if (aabb.Collidable.IsHardCollidable) //If the collider is meant to be collidable at the moment
                {
                    collided = true;

                    if (bump)
                    {
                        aabb.Collidable.Bumped(collider);
                        collidedwith.Add(aabb.Collidable.Owner);
                    }
                }
            }

            colliderComponent.Bump(collidedwith);

            //TODO: This needs multi-grid support.
            return collided;
        }

        /// <summary>
        ///     Adds a collidable to the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void AddCollidable(ICollidable collidable)
        {
            if (_aabbs.ContainsKey(collidable))
            {
                // TODO: throw an exception instead.
                // There's too much buggy code in the client that I can't be bothered to fix,
                // so it'd crash reliably.
                UpdateCollidable(collidable);
                return;
            }
            var c = new CollidableAABB(collidable);
            foreach (var p in c.Points)
            {
                AddPoint(p);
            }
            var comp = collidable as IComponent;
            _aabbs.Add(collidable, (aabb: c, owner: comp?.Owner));
        }

        /// <summary>
        ///     Removes a collidable from the manager
        /// </summary>
        /// <param name="collidable"></param>
        public void RemoveCollidable(ICollidable collidable)
        {
            var ourAABB = _aabbs[collidable].aabb;

            foreach (var p in ourAABB.Points)
            {
                RemovePoint(p);
            }
            _aabbs.Remove(collidable);
        }

        /// <summary>
        ///     Updates the collidable in the manager.
        /// </summary>
        /// <param name="collidable"></param>
        public void UpdateCollidable(ICollidable collidable)
        {
            RemoveCollidable(collidable);
            AddCollidable(collidable);
        }

        /// <summary>
        ///     Adds an AABB point to a buckets
        /// </summary>
        /// <param name="point"></param>
        private void AddPoint(CollidablePoint point)
        {
            var b = GetBucket(point.Coordinates);
            b.AddPoint(point);
        }

        /// <summary>
        ///     Removes an AABB point from a bucket
        /// </summary>
        /// <param name="point"></param>
        private void RemovePoint(CollidablePoint point)
        {
            var b = GetBucket(point.Coordinates);
            b.RemovePoint(point);
        }

        public RayCastResults IntersectRay(Ray ray, float maxLength = 50, IEntity entityignore = null)
        {
            var closestResults = new RayCastResults(float.PositiveInfinity, Vector2.Zero, null);
            var minDist = float.PositiveInfinity;
            var localBounds = new Box2(0, BucketSize, BucketSize, 0);

            // for each bucket index
            foreach (var kvIndices in _bucketIndex)
            {
                var worldBounds = localBounds.Translated(kvIndices.Key * BucketSize);

                // check if ray intersects the bucket AABB
                if (ray.Intersects(worldBounds, out var dist, out _))
                {
                    // bucket is too far away
                    if (dist > maxLength)
                        continue;

                    // get the object it intersected in the bucket
                    var bucket = _buckets[kvIndices.Value];
                    if (TryGetClosestIntersect(ray, bucket, out var results, entityignore))
                    {
                        if (results.Distance < minDist)
                        {
                            minDist = results.Distance;
                            closestResults = results;
                        }
                    }
                }
            }

            return closestResults;
        }

        /// <summary>
        ///     Return the closest object, inside a bucket, to the ray origin that was intersected (if any).
        /// </summary>
        private static bool TryGetClosestIntersect(Ray ray, CollidableBucket bucket, out RayCastResults results, IEntity entityignore = null)
        {
            IEntity entity = null;
            var hitPosition = Vector2.Zero;
            var minDist = float.PositiveInfinity;

            foreach (var collidablePoint in bucket.GetPoints()) // *goes to kitchen to freshen up his drink...*
            {
                var worldAABB = collidablePoint.ParentAABB.Collidable.WorldAABB;

                if (ray.Intersects(worldAABB, out var dist, out var hitPos) && !(dist > minDist))
                {
                    if (entityignore != null && entityignore == collidablePoint.ParentAABB.Collidable.Owner)
                    {
                        continue;
                    }

                    entity = collidablePoint.ParentAABB.Collidable.Owner;
                    minDist = dist;
                    hitPosition = hitPos;
                }
            }

            if (minDist < float.PositiveInfinity)
            {
                results = new RayCastResults(minDist, hitPosition, entity);
                return true;
            }

            results = default(RayCastResults);
            return false;
        }

        /// <summary>
        ///     Gets a bucket given a point coordinate
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        private CollidableBucket GetBucket(Vector2 coordinate)
        {
            var key = GetBucketCoordinate(coordinate);
            return _bucketIndex.ContainsKey(key)
                ? _buckets[_bucketIndex[key]]
                : CreateBucket(key);
        }

        private static Vector2i GetBucketCoordinate(Vector2 coordinate)
        {
            var x = (int)Math.Floor(coordinate.X / BucketSize);
            var y = (int)Math.Floor(coordinate.Y / BucketSize);
            return new Vector2i(x, y);
        }

        private CollidableBucket CreateBucket(Vector2i coordinate)
        {
            if (_bucketIndex.ContainsKey(coordinate))
                return _buckets[_bucketIndex[coordinate]];

            var b = new CollidableBucket(_lastIndex, coordinate);
            _buckets.Add(_lastIndex, b);
            _bucketIndex.Add(coordinate, _lastIndex);
            _lastIndex++;
            return b;
        }
    }
}
