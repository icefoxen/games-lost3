
using System;

namespace Lost {

	public class Background {
		Mesh mesh;
		public Background(string tex) {
			Vbo geom = Loader.GetGeom("background.obj");
			uint tex = Loader.GetTex(tex);
			mesh = new Mesh(ref Vbo, tex);
		}
		
		public void Draw(Vector3d loc) {
			mesh.Draw(loc);
		}
	}
}
