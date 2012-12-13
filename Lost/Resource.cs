using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using OpenTK.Graphics.OpenGL;

using Meshomatic;

namespace Lost {
	class Memoizer<T> {
		Func<string,T> loader;
		Dictionary<string,T> cache;
		
		public Memoizer(Func<string,T> f) {
			cache = new Dictionary<string,T>();
			loader = f;
		}
		public T Get(string name) {
			try {
				return cache[name];
			} catch(KeyNotFoundException) {
				Console.WriteLine("Loading {0}", name);
				try {
					T t = loader(name);
					cache.Add(name, t);
					return t;
				} catch {
					Console.WriteLine("Error loading {0}!", name);
					throw;
				}
			}
		}
	}
	
	public class Loader {
		static Memoizer<uint> texloader;
		static Memoizer<Vbo> geomloader;
		static Memoizer<int> soundloader;
		static Memoizer<XmlDocument> configloader;
		static Memoizer<Mesh> billboardloader;
		static Memoizer<Mesh> meshloader;
		static string datadir = "../../../data/";
		
		public static void Init() {
			texloader = new Memoizer<uint>(file => LoadTex(file));
			geomloader = new Memoizer<Vbo>(file => LoadGeom(file));
			soundloader = new Memoizer<int>(file => LoadSound(file));
			configloader = new Memoizer<XmlDocument>(file => LoadConfig(file));
			billboardloader = new Memoizer<Mesh>(file => LoadBillboard(file));
			meshloader = new Memoizer<Mesh>(file => LoadMesh(file));
		}
		public static uint GetTex(string file) {
			return texloader.Get(datadir + file);
		}
		public static Vbo GetGeom(string file) {
			return geomloader.Get(datadir + file);
		}
		public static int GetSound(string file) {
			return soundloader.Get(datadir + file);
		}
		public static XmlDocument GetConfig(string file) {
			return configloader.Get(datadir + file);
		}
		// This uses GetConfig, so it already has the datadir attached.
		public static Mesh GetBillboard(string file) {
			return billboardloader.Get(file);
		}
		public static Mesh GetMesh(string file) {
			return meshloader.Get(file);
		}
		
		
		public static uint LoadTex(string file) {
			Bitmap bitmap = new Bitmap(file);
			if(!Misc.IsPowerOf2(bitmap.Width) || !Misc.IsPowerOf2(bitmap.Height)) {
				// XXX: FormatException isn't really the best here, buuuut...
				throw new FormatException("Texture sizes must be powers of 2!");
			}
			uint texture;
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);
		

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			
			return texture;
		}

		static Vbo LoadGeom(string file) {
			MeshData m = new ObjLoader().LoadFile(file);
			float[] verts; float[] norms; float[] texcoords; uint[] indices;
			m.OpenGLArrays(out verts, out norms, out texcoords, out indices);
			
			bool v = false;
			for(int i = 0; i < texcoords.Length; i++) {
				if(v) {
					texcoords[i] = 1 - texcoords[i];
					v = false;
				} else {
					v = true;
				}
			}
			return new Vbo(verts, norms, texcoords, indices);
		}
		
		static int LoadSound(string file) {
			return 0;
		}
		
		static XmlDocument LoadConfig(string file) {
			XmlDocument d = new XmlDocument();
			using(FileStream f = new FileStream(file, FileMode.Open)) {
				d.Load(f);
			}
			return d;
		}
		
		// This takes an XML file containing config info.
		static Mesh LoadBillboard(string file) {
			XmlDocument config = Loader.GetConfig(file);
			XmlNode geom = config.SelectSingleNode("/bb/geom");
			double w = Double.Parse(geom.Attributes["x"].Value);
			double h = Double.Parse(geom.Attributes["y"].Value);
			
			XmlNode tex = config.SelectSingleNode("/bb/texture");
			Vbo bgGeom = Loader.GetGeom("billboard.obj");
			uint t = Loader.GetTex(tex.InnerText);
			XmlNode mat = config.SelectSingleNode("/bb/material");
			uint m = Loader.GetTex(mat.InnerText);
			XmlNode glow = config.SelectSingleNode("/bb/glow");
			uint g = Loader.GetTex(glow.InnerText);
			
			Material matl = new Material(t, m, g);
			
			return new Mesh(bgGeom, matl, new OpenTK.Vector2d(w, h));
		}
		
		static Mesh LoadMesh(string file) {
			XmlDocument config = Loader.GetConfig(file);
			XmlNode geom = config.SelectSingleNode("/mesh/geom");
			Vbo g = Loader.GetGeom(geom.InnerText);
			
			XmlNode tex = config.SelectSingleNode("/mesh/texture");
			uint t = Loader.GetTex(tex.InnerText);
			XmlNode mat = config.SelectSingleNode("/mesh/material");
			uint m = Loader.GetTex(mat.InnerText);
			XmlNode glow = config.SelectSingleNode("/mesh/glow");
			uint gl = Loader.GetTex(glow.InnerText);
			
			Material matl = new Material(t, m, gl);
			
			return new Mesh(g, matl);
		}
	}
}