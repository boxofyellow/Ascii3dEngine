namespace Ascii3dEngine.Engine
{
    public class Model : PolygonActorBase
    {
        public Model(Settings settings) 
            : base(settings, 
                WaveObjFormParser.Parse(settings.ModelFile!)) { }
    }
}