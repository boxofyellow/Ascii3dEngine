namespace Ascii3dEngine
{
    public class Model : PolygonActorBase
    {
        public Model(Settings settings) : base(settings) { }

        protected override (Point3D[] Points, int[][] Faces) GetData(Settings settings) 
            => WaveObjFormParser.Parse(settings.ModelFile);
    }
}