# CollideWith (draft document)

The CollideWith buffer component is used to limit raycasting and collisions to some physics entities.
One of the most notable usage would be in a game with multiple team of players:
- You may not want the team to be able to collide with eachother (bullets and characters).
- But you want them to be able to collide with ennemies bullets and characters.
  
There are some solutions for that:
- A layer system: If you have a lot of constraints (like, a lot of teams), this decision would may not be good.
- Disabling/Renabling colliders: This can cost a lot of performance (especially when there is a lot of colliders).
- Assigning an unique id between colliders: This may be one of the best solution, but you need to wait for the physic engine to automatically update colliders data, this may be fine in some games, but in games like AFPS where everything can have multiple positions in the same frame, this is a bad idea.

The CollideWith component is one of the best solution (because there can be other good solutions, but I didn't found them yet).

An element of the CollideWith component is structured like this:
| Name | Type | Description |
| -- | -- | -- |
| Target | Entity | An entity where we can do some raycasting/collisions operations. |
| ColliderPtr | Collider* | An automatically generated field about the current entity collider. |
| WorldFromMotion | RigidTransform | An automatically generated field about the transformed collider. |

If you want your character to collide with a bullet:
```csharp
void MakeTheBulletCollidable(in Entity character, in Entity bullet)
{
    var cwBuffer = EntityManager.GetBuffer<CollideWith>(character);

    cwBuffer.Add(new CollideWith { Target = bullet });
}
```

To make things fast, there is a system called `TransformCollideWithBufferSystem` (should be updated in Initialization group), it generate the required field values of `CollideWith` for when you want to do your stuff. 

It may be possible that some stuff could be incorrect when you make some raycast, one the reason would be that you moved some entities (that are also located in `CollideWith` buffer), one of the first fix would be to call `TransformCollideWithBufferSystem.Update()`.

# Basic example: raycasting

```csharp
struct MyRaycastJob : JobProcessComponentDataForEntity<Translation>
{
    public BufferFromEntity<CollideWith> CollideWithBufferArray;
    public NativeList<RaycastHit> Hits;

    public Execute(Entity entity, int index, ref Translation translation)
    {
        var cwBuffer = CollideWithBufferArray[entity];
        var rayInput = new RaycastInput
        {
            // we just make a simple raycast from the position to the right.
            Ray    = new Ray(translation.Value, new float3(1, 0, 0)),
            Filter = CollisionFilter.Default
        };

        // start the raycasting...
        if (cwBuffer.CastRay(rayInput, out var closestHit))
        {
            // we got a hit, yeah!
            Hits.add(closestHit);
        }
    }
}


protected override void OnUpdate()
{
    var job = new MyRaycastJob
    {
        CollideWithBufferArray = GetBufferFromEntity<CollideWith>(),
        Hits = new NativeList<RaycastHit>(Allocator.TempJob)
    };

    job.Schedule(this);
    job.Complete();

    for (var i = 0; i != Hits.Length; i++)
    {
        Debug.Log(Hits[i].Position)
    }
}
```