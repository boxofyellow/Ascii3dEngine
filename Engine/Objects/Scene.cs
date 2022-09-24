using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;

public class Scene
{

    public Scene(Camera camera, Point2D size)
    {
        Screen = new(size);
        Camera = camera;
    }

    public readonly Camera Camera;
    public readonly Screen Screen;

    public void AddActor(Actor actor) => m_actors.Add(actor);

    public void AddLightSource(LightSource source) => m_lightSources.Add(source);

    public void Act(TimeSpan timeDelta, TimeSpan elapsedRuntime)
    {
        foreach (var actor in CollectionsMarshal.AsSpan(m_actors))
        {
            actor.Act(timeDelta, elapsedRuntime, Camera);
        }
        foreach  (var source in CollectionsMarshal.AsSpan(m_lightSources))
        {
            source.Act(timeDelta, elapsedRuntime, Camera);
        }
    }

    public Rgb24[,] RenderCharRayColor(Point2D size, CharMap map, int maxDegreeOfParallelism = -1) 
        => RayTracer.TraceColor(Utilities.Ratio(size.H, map.MaxX), Utilities.Ratio(size.V, map.MaxY), this, m_actors, m_lightSources, maxDegreeOfParallelism);

    public bool HasActors => m_actors.Any();

    private readonly List<Actor> m_actors = new();
    private readonly List<LightSource> m_lightSources = new();
}