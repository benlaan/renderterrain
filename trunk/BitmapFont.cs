// BitmapFont.cs
// Bitmap Font class for XNA
// Copyright 2006 Microsoft Corp.
// Gary Kacmarcik (garykac@microsoft.com)
// Revision: 2006-Nov-18

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace XNAExtras
{
	/// <summary>
	/// Bitmap font class for XNA
	/// </summary>
    public class BitmapFont : Microsoft.Xna.Framework.DrawableGameComponent
	{
		public enum TextAlignment
		{
			Left,
			Center,
			Right,
		}

		/// <summary>
		/// Save font rendering state so that it can be saved/restored
		/// </summary>
		public struct SaveStateInfo
		{
			public bool fKern;
			public float fpDepth;
			public TextAlignment align;
			public Vector2 vPen;
			public Color color;
		}

		enum GlyphFlags
		{
			None = 0,
			ForceWhite = 1,		// force the drawing color for this glyph to be white.
		}

		/// <summary>
		/// Info for each glyph in the font - where to find the glyph image and 
		/// other properties
		/// </summary>
		struct GlyphInfo
		{
			public ushort nBitmapID;
			public byte pxLocX;
			public byte pxLocY;
			public byte pxWidth;
			public byte pxHeight;
			public byte pxAdvanceWidth;
			public sbyte pxLeftSideBearing;
			public GlyphFlags nFlags;
		}

		/// <summary>
		/// Info for each font bitmap
		/// </summary>
		struct BitmapInfo
		{
			public string strFilename;
			public int nX, nY;
		}

		private SpriteBatch _sb;
		private SpriteBatch _sbOverride;
		private ContentManager _content;
		private string _strName;
		private string _strPath;
		private string _strFilename;
		private bool _fLoadFromResource;
		private Dictionary<int, BitmapInfo> _dictBitmapID2BitmapInfo;
		private Dictionary<int, Texture2D> _dictBitmapID2Texture;
		private Dictionary<char, GlyphInfo> _dictUnicode2GlyphInfo;
		private Dictionary<char, Dictionary<char, sbyte>> _dictKern;
		private int _nBase = 0;
		private int _nHeight = 0;
		private float _fpDepth = 0.0f;
		private TextAlignment _eAlign = TextAlignment.Left;

		/// <summary>
		/// A dictionary of all currently defined fonts
		/// </summary>
		private static Dictionary<string, BitmapFont> _dictBitmapFonts = new Dictionary<string,BitmapFont>();

#if true	// remove this code (or set to false) to compile for Xbox360
		/// <summary>
		/// Create a new font from the info in the specified font descriptor (XML) file
		/// </summary>
		/// <param name="strFontFilename">Font descriptor file (.xml)</param>
		public BitmapFont(string strFontFilename, Game game) : base(game)
		{
			Init(null, strFontFilename);
		}
#endif

		/// <summary>
		/// Create a new font from the info in the specified font descriptor (XML) file
		/// </summary>
		/// <param name="cm">Content manager</param>
		/// <param name="strFontFilename">Font descriptor file (.xml)</param>
        public BitmapFont(ContentManager cm, string strFontFilename, Game game)
            : base(game)
		{
			Init(cm, strFontFilename);
		}

		private void Init(ContentManager cm, string strFontFilename)
		{
			_content = cm;
			_sb = null;
			_sbOverride = null;

			_dictBitmapID2BitmapInfo = new Dictionary<int, BitmapInfo>();
			_dictBitmapID2Texture = new Dictionary<int, Texture2D>();

			_dictUnicode2GlyphInfo = new Dictionary<char, GlyphInfo>();
			_dictKern = new Dictionary<char, Dictionary<char, sbyte>>();

			XmlDocument xd = new XmlDocument();
			
			// load the XML file from wherever it is - filesystem or embedded
			if (System.IO.File.Exists(strFontFilename))
			{
				// all files mentioned in the font descriptor file are relative to 
                // the parent directory of the font file.
				// record the path to this directory.
				_strPath = System.IO.Path.GetDirectoryName(strFontFilename);
				if (_strPath != "")
					_strPath += @"/";
				_strFilename = strFontFilename;	// relative path + filename
				_fLoadFromResource = false;

				xd.Load(strFontFilename);
			}
			else
			{
				// look in the assembly resources
				bool fFoundResource = false;
				string strEmbeddedPath, strEmbeddedName;
				ConvertFilePath2EmbeddedPath(strFontFilename, out strEmbeddedPath, out strEmbeddedName);
				System.IO.Stream ios = Assembly.GetExecutingAssembly().GetManifestResourceStream(strEmbeddedPath + strEmbeddedName);
				if (ios != null)
				{
					_strPath = strEmbeddedPath;		// path to resource
					_strFilename = strEmbeddedName;	// resource name
					_fLoadFromResource = true;
					fFoundResource = true;

					xd.Load(ios);
				}
				if (!fFoundResource)
					throw new System.Exception(String.Format("Unable to find font named '{0}'.", strFontFilename));
			}

			_strName = "";

			LoadFontXML(xd.ChildNodes);

			// if the font doesn't define a name, create one from the filename
			if (_strName == "")
				_strName = System.IO.Path.GetFileNameWithoutExtension(strFontFilename);

            if (!_dictBitmapFonts.ContainsKey(_strName))
			    // add this font to the list of active fonts
			    _dictBitmapFonts.Add(_strName, this);
		}

		void ConvertFilePath2EmbeddedPath(string strFilePath, out string strEmbeddedPath, out string strEmbeddedName)
		{
			Assembly a = Assembly.GetExecutingAssembly();
			// calc the resource path to this font (strip off <fontname>.xml)
			strEmbeddedPath = a.GetName().Name + ".";

			// strip directory nams from the filepath and add them to the embedded path
			string[] aPath = strFilePath.Split(new char[] { '/', '\\' });
			for (int i = 0; i < aPath.Length - 1; i++)
				strEmbeddedPath += aPath[i] + ".";
			strEmbeddedName = aPath[aPath.Length-1];
		}

		/// <summary>
		/// Destructor for BitmapFont. Remove font from list of active fonts.
		/// </summary>
		~BitmapFont()
		{
			Dispose();
			_dictBitmapFonts.Remove(_strName);
		}

		/// <summary>
		/// Dispose of all of the non-managed resources for this object
		/// </summary>
        //public override void Dispose()
        //{
        //    _sb.Dispose();
        //    foreach (int key in _dictBitmapID2Texture.Keys)
        //        _dictBitmapID2Texture[key].Dispose();
        //}

		/// <summary>
		/// Reset the font when the device has changed
		/// </summary>
		/// <param name="device">The new device</param>
        public override void Initialize()
        {
            base.Initialize();

			Assembly a = Assembly.GetExecutingAssembly();
			_sb = new SpriteBatch(GraphicsDevice);

			foreach (KeyValuePair<int, BitmapInfo> kv in _dictBitmapID2BitmapInfo)
			{
				Texture2D tex = null;

				// use the ContentManager if one was specified
				if (_content != null)
					tex = _content.Load<Texture2D>(_strPath + kv.Value.strFilename);

				// otherwise, load from the filesystem
				else
				{
#if true	// remove this code (or set to false) to compile for Xbox360
					TextureCreationParameters tcp = TextureCreationParameters.Default;
					tcp.Width = kv.Value.nX;
					tcp.Height = kv.Value.nY;
					if (_fLoadFromResource)
					{
						System.IO.Stream s = a.GetManifestResourceStream(_strPath + kv.Value.strFilename);
						tex = Texture2D.FromFile(GraphicsDevice, s, tcp) as Texture2D;
					}
					else
						tex = Texture2D.FromFile(GraphicsDevice, _strPath + kv.Value.strFilename, tcp);
#endif
				}

				_dictBitmapID2Texture[kv.Key] = tex;
			}
		}

		/// <summary>
		/// The name of this font
		/// </summary>
		public string Name
		{
			get { return _strName; }
		}

		/// <summary>
		/// The name of the font file
		/// </summary>
		public string Filename
		{
			get { return _strFilename; }
		}

		/// <summary>
		/// Should we kern adjacent characters?
		/// </summary>
		private bool _fKern = true;

		/// <summary>
		/// Enable/disable kerning
		/// </summary>
		public bool KernEnable
		{
			get { return _fKern; }
			set { _fKern = value; }
		}

		/// <summary>
		/// Distance from top of font to the baseline
		/// </summary>
		public int Baseline
		{
			get { return _nBase; }
		}

		/// <summary>
		/// Distance from top to bottom of the font
		/// </summary>
		public int LineHeight
		{
			get { return _nHeight; }
		}

		/// <summary>
		/// The depth at which to draw the font
		/// </summary>
		public float Depth
		{
			get { return _fpDepth; }
			set { _fpDepth = value; }
		}

		/// <summary>
		/// The text alignment. This is only used by the TextBox routines
		/// </summary>
		public TextAlignment Alignment
		{
			get { return _eAlign; }
			set { _eAlign = value; }
		}

		/// <summary>
		/// Calculate the width of the given string.
		/// </summary>
		/// <param name="format">String format</param>
		/// <param name="args">String format arguments</param>
		/// <returns>Width (in pixels) of the string</returns>
		public int MeasureString(string format, params object[] args)
		{
			string str = string.Format(format, args);
			int pxWidth = 0;
			char cLast = '\0';

			foreach (char c in str)
			{
				if (!_dictUnicode2GlyphInfo.ContainsKey(c))
				{
					//TODO: print out undefined char glyph
					continue;
				}

				GlyphInfo ginfo = _dictUnicode2GlyphInfo[c];

				// if kerning is enabled, get the kern adjustment for this char pair
				if (_fKern)
				{
					pxWidth += CalcKern(cLast, c);
					cLast = c;
				}

				// update the string width
				pxWidth += ginfo.pxAdvanceWidth;
			}

			return pxWidth;
		}

		/// <summary>
		/// Calculate the number of characters that fit in the given width.
		/// </summary>
		/// <param name="pxMaxWidth">Maximum string width</param>
		/// <param name="str">String</param>
		/// <param name="nChars">Number of characters that fit</param>
		/// <param name="pxWidth">Width of substring</param>
		private void CountCharWidth(int pxMaxWidth, string str, out int nChars, out int pxWidth)
		{
			int nLastWordBreak = 0;
			int pxLastWordBreakWidth = 0;
			int pxLastWidth = 0;
			char cLast = '\0';

			nChars = 0;
			pxWidth = 0;

			foreach (char c in str)
			{
				// if this is a newline, then return. the width is set correctly
				if (c == '\n')
				{
					nChars++;
					return;
				}

				if (!_dictUnicode2GlyphInfo.ContainsKey(c))
				{
					//TODO: print out undefined char glyph
					continue;
				}

				GlyphInfo ginfo = _dictUnicode2GlyphInfo[c];

				// if kerning is enabled, get the kern adjustment for this char pair
				if (_fKern)
				{
					int pxKern = CalcKern(cLast, c);
					pxWidth += pxKern;
					cLast = c;
				}

				// update the string width and char count
				pxLastWidth = pxWidth;
				pxWidth += ginfo.pxAdvanceWidth;
				nChars++;

				// record the end of the previous word if this is a whitespace char
				if (Char.IsWhiteSpace(c))
				{
					nLastWordBreak = nChars;			// include space in char count
					pxLastWordBreakWidth = pxLastWidth;	// don't include space in width
				}

				// if we've exceeded the max, then return the chars up to the last complete word
				if (pxWidth > pxMaxWidth)
				{
					pxWidth = pxLastWordBreakWidth;
					if (pxWidth == 0)
					{
						// fallback to last char if we haven't seen a complete word
						pxWidth = pxLastWidth;
						nChars--;
					}
					else
						nChars = nLastWordBreak;
					return;
				}
			}
		}

		/// <summary>
		/// Current pen position
		/// </summary>
		private Vector2 _penPosition = new Vector2(0, 0);

		/// <summary>
		/// Current pen position
		/// </summary>
		public Vector2 Pen
		{
			get { return _penPosition; }
			set { _penPosition = value; }
		}

		/// <summary>
		/// Set the current pen position
		/// </summary>
		/// <param name="x">X-coord</param>
		/// <param name="y">Y-coord</param>
		public void SetPen(int x, int y)
		{
			_penPosition = new Vector2(x, y);
		}


		/// <summary>
		/// Current color used for drawing text
		/// </summary>
		private Color _color = Color.White;

		/// <summary>
		/// Current color used for drawing text
		/// </summary>
		public Color TextColor
		{
			get { return _color; }
			set { _color = value; }
		}


		/// <summary>
		/// Draw the given string at (x,y).
		/// The text color is inherited from the last draw command (default=White).
		/// </summary>
		/// <param name="x">X-coord</param>
		/// <param name="y">Y-coord</param>
		/// <param name="format">String format</param>
		/// <param name="args">String format args</param>
		/// <returns>Width of string (in pixels)</returns>
		public int DrawString(int x, int y, string format, params object[] args)
		{
			Vector2 v = new Vector2(x, y);
			return DrawString(v, _color, format, args);
		}

		/// <summary>
		/// Draw the given string at (x,y) using the specified color
		/// </summary>
		/// <param name="x">X-coord</param>
		/// <param name="y">Y-coord</param>
		/// <param name="color">Text color</param>
		/// <param name="format">String format</param>
		/// <param name="args">String format args</param>
		/// <returns>Width of string (in pixels)</returns>
		public int DrawString(int x, int y, Color color, string format, params object[] args)
		{
			Vector2 v = new Vector2(x, y);
			return DrawString(v, color, format, args);
		}

		/// <summary>
		/// Draw the given string using the specified color.
		/// The text drawing location is immediately after the last drawn text (default=0,0).
		/// </summary>
		/// <param name="color">Text color</param>
		/// <param name="format">String format</param>
		/// <param name="args">String format args</param>
		/// <returns>Width of string (in pixels)</returns>
		public int DrawString(Color color, string format, params object[] args)
		{
			return DrawString(_penPosition, color, format, args);
		}

		/// <summary>
		/// Draw the given string at (x,y).
		/// The text drawing location is immediately after the last drawn text (default=0,0).
		/// The text color is inherited from the last draw command (default=White).
		/// </summary>
		/// <param name="format">String format</param>
		/// <param name="args">String format args</param>
		/// <returns>Width of string (in pixels)</returns>
		public int DrawString(string format, params object[] args)
		{
			return DrawString(_penPosition, _color, format, args);
		}

		/// <summary>
		/// Draw the given string at vOrigin using the specified color
		/// </summary>
		/// <param name="vAt">(x,y) coord</param>
		/// <param name="cText">Text color</param>
		/// <param name="strFormat">String format</param>
		/// <param name="args">String format args</param>
		/// <returns>Width of string (in pixels)</returns>
		public int DrawString(Vector2 vAt, Color cText, string strFormat, params object[] args)
		{
			string str = string.Format(strFormat, args);

			return InternalDrawString(vAt, cText, str);
		}

		/// <summary>
		/// Private version of DrawString that expects the string to be formatted already
		/// </summary>
		/// <param name="vAt">(x,y) coord</param>
		/// <param name="cText">Text color</param>
		/// <param name="str">String</param>
		/// <returns>Width of string (in pixels)</returns>
		private int InternalDrawString(Vector2 vAt, Color cText, string str)
		{
			Vector2 vOrigin = new Vector2(0,0);
			int pxWidth = 0;
			char cLast = '\0';

			// are we using our local SpriteBatch, or an override?
			bool fSBOverride = (_sbOverride != null);
			SpriteBatch sb = (fSBOverride ? _sbOverride : _sb);

			if (!fSBOverride)
				sb.Begin(SpriteBlendMode.AlphaBlend);

			// draw each character in the string
			foreach (char c in str)
			{
				if (!_dictUnicode2GlyphInfo.ContainsKey(c))
				{
					//TODO: print out undefined char glyph
					continue;
				}

				GlyphInfo ginfo = _dictUnicode2GlyphInfo[c];

				// if kerning is enabled, get the kern adjustment for this char pair
				if (_fKern)
				{
					int pxKern = CalcKern(cLast, c);
					vAt.X += pxKern;
					pxWidth += pxKern;
					cLast = c;
				}
	
				// draw the glyph
				vAt.X += ginfo.pxLeftSideBearing;
				if (ginfo.pxWidth != 0 && ginfo.pxHeight != 0)
				{
					Rectangle rSource = new Rectangle(ginfo.pxLocX, ginfo.pxLocY, ginfo.pxWidth, ginfo.pxHeight);
					Color color = (((ginfo.nFlags & GlyphFlags.ForceWhite) != 0) ? Color.White : cText);
					sb.Draw(
                        _dictBitmapID2Texture[ginfo.nBitmapID], 
                        vAt, 
                        rSource, 
                        color, 
                        0.0f, 
                        vOrigin, 
                        1.0f, 
                        SpriteEffects.None, 
                        _fpDepth
                    );
				}

				// update the string width and advance the pen to the next drawing position
				pxWidth += ginfo.pxAdvanceWidth;
				vAt.X += ginfo.pxAdvanceWidth - ginfo.pxLeftSideBearing;
			}

			if (!fSBOverride)
				sb.End();

			// record final pen position and color
			_penPosition = vAt;
			_color = cText;

			return pxWidth;
		}

		/// <summary>
		/// Get the kern value for the given pair of characters
		/// </summary>
		/// <param name="chLeft">Left character</param>
		/// <param name="chRight">Right character</param>
		/// <returns>Amount to kern (in pixels)</returns>
		private int CalcKern(char chLeft, char chRight)
		{
			if (_dictKern.ContainsKey(chLeft))
			{
				Dictionary<char, sbyte> kern2 = _dictKern[chLeft];
				if (kern2.ContainsKey(chRight))
					return kern2[chRight];
			}
			return 0;
		}

		/// <summary>
		/// Draw text formatted to fit in the specified rectangle
		/// </summary>
		/// <param name="r">The rectangle to fit the text</param>
		/// <param name="cText">Text color</param>
		/// <param name="strFormat">String format</param>
		/// <param name="args">String format args</param>
		public void TextBox(Rectangle bounds, Color color, string text)
		{
			int nChars;
			int pxWidth;
			Vector2 vAt = new Vector2(bounds.Left, bounds.Top);

			while (text.Length != 0)
			{
				// stop drawing if there isn't room for this line
				if (vAt.Y + _nHeight > bounds.Bottom)
					return;

				CountCharWidth(bounds.Width, text, out nChars, out pxWidth);

				switch (_eAlign)
				{
					case TextAlignment.Left:
						vAt.X = bounds.Left;
						break;
					case TextAlignment.Center:
						vAt.X = bounds.Left + ((bounds.Width - pxWidth) / 2);
						break;
					case TextAlignment.Right:
						vAt.X = bounds.Left + (bounds.Width - pxWidth);
						break;
				}
				InternalDrawString(vAt, color, text.Substring(0, nChars));
				text = text.Substring(nChars);
				vAt.Y += _nHeight;
			}
		}

		/// <summary>
		/// Save the current font rendering state
		/// </summary>
		/// <param name="bfss">Struct to store the save state</param>
		public void SaveState(out SaveStateInfo bfss)
		{
			bfss.fKern = _fKern;
			bfss.fpDepth = _fpDepth;
			bfss.align = _eAlign;
			bfss.vPen = _penPosition;
			bfss.color = _color;
		}

		/// <summary>
		/// Restore the font rendering state
		/// </summary>
		/// <param name="bfss">Previously saved font state</param>
		public void RestoreState(SaveStateInfo bfss)
		{
			_fKern = bfss.fKern;
			_fpDepth = bfss.fpDepth;
			_eAlign = bfss.align;
			_penPosition = bfss.vPen;
			_color = bfss.color;
		}

		/// <summary>
		/// Temporarily override the font's SpriteBatch with the given SpriteBatch.
		/// </summary>
		/// <param name="sb">The new SpriteBatch (or null to reset)</param>
		/// <remarks>
		/// When drawing text using the SpriteBatch override, Begin/End will not be called on the SpriteBatch.
		/// Use null to reset back to the font's original SpriteBatch.
		/// </remarks>
		public void SpriteBatchOverride(SpriteBatch sb)
		{
			_sbOverride = sb;
		}

		/// <summary>
		/// Return the font associated with the given name.
		/// </summary>
		/// <param name="strName">Name of the font</param>
		/// <returns>The font</returns>
		public static BitmapFont GetNamedFont(string strName)
		{
			return _dictBitmapFonts[strName];
		}

		#region Load Font from XML

		/// <summary>
		/// Load the font data from an XML font descriptor file
		/// </summary>
		/// <param name="xnl">XML node list containing the entire font descriptor file</param>
		private void LoadFontXML(XmlNodeList xnl)
		{
			foreach (XmlNode xn in xnl)
			{
				if (xn.Name == "font")
				{
					_strName = GetXMLAttribute(xn, "name");
					_nBase = Int32.Parse(GetXMLAttribute(xn, "base"));
					_nHeight = Int32.Parse(GetXMLAttribute(xn, "height"));

					LoadFontXML_font(xn.ChildNodes);
				}
			}
		}

		/// <summary>
		/// Load the data from the "font" node
		/// </summary>
		/// <param name="xnl">XML node list containing the "font" node's children</param>
		private void LoadFontXML_font(XmlNodeList xnl)
		{
			foreach (XmlNode xn in xnl)
			{
				if (xn.Name == "bitmaps")
					LoadFontXML_bitmaps(xn.ChildNodes);
				if (xn.Name == "glyphs")
					LoadFontXML_glyphs(xn.ChildNodes);
				if (xn.Name == "kernpairs")
					LoadFontXML_kernpairs(xn.ChildNodes);
			}
		}

		/// <summary>
		/// Load the data from the "bitmaps" node
		/// </summary>
		/// <param name="xnl">XML node list containing the "bitmaps" node's children</param>
		private void LoadFontXML_bitmaps(XmlNodeList xnl)
		{
			foreach (XmlNode xn in xnl)
			{
				if (xn.Name == "bitmap")
				{
					string strID = GetXMLAttribute(xn, "id");
					string strFilename = GetXMLAttribute(xn, "name");
					string strSize = GetXMLAttribute(xn, "size");
					string[] aSize = strSize.Split('x');
					
					// if the ContentManager is being used, then we may need to strip off the .png extension
					// to generate the correct asset name.
					if (_content != null && strFilename.EndsWith(@".png"))
						strFilename = strFilename.Remove(strFilename.Length - 4, 4);

					BitmapInfo bminfo;
					bminfo.strFilename = strFilename;
					bminfo.nX = Int32.Parse(aSize[0]);
					bminfo.nY = Int32.Parse(aSize[1]);

					_dictBitmapID2BitmapInfo[Int32.Parse(strID)] = bminfo;
				}
			}
		}

		/// <summary>
		/// Load the data from the "glyphs" node
		/// </summary>
		/// <param name="xnl">XML node list containing the "glyphs" node's children</param>
		private void LoadFontXML_glyphs(XmlNodeList xnl)
		{
			foreach (XmlNode xn in xnl)
			{
				if (xn.Name == "glyph")
				{
					string strChar = GetXMLAttribute(xn, "ch");
					string strBitmapID = GetXMLAttribute(xn, "bm");
					string strLoc = GetXMLAttribute(xn, "loc");
					string strSize = GetXMLAttribute(xn, "size");
					string strAW = GetXMLAttribute(xn, "aw");
					string strLSB = GetXMLAttribute(xn, "lsb");
					string strForceWhite = GetXMLAttribute(xn, "forcewhite");

					if (strLoc == "")
						strLoc = GetXMLAttribute(xn, "origin");	// obsolete - use loc instead

					string[] aLoc = strLoc.Split(',');
					string[] aSize = strSize.Split('x');

					GlyphInfo ginfo = new GlyphInfo();
					ginfo.nBitmapID = UInt16.Parse(strBitmapID);
					ginfo.pxLocX = Byte.Parse(aLoc[0]);
					ginfo.pxLocY = Byte.Parse(aLoc[1]);
					ginfo.pxWidth = Byte.Parse(aSize[0]);
					ginfo.pxHeight = Byte.Parse(aSize[1]);
					ginfo.pxAdvanceWidth = Byte.Parse(strAW);
					ginfo.pxLeftSideBearing = SByte.Parse(strLSB);
					ginfo.nFlags = 0;
					ginfo.nFlags |= (strForceWhite == "true" ? GlyphFlags.ForceWhite : GlyphFlags.None);

					_dictUnicode2GlyphInfo[strChar[0]] = ginfo;
				}
			}
		}

		/// <summary>
		/// Load the data from the "kernpairs" node
		/// </summary>
		/// <param name="xnl">XML node list containing the "kernpairs" node's children</param>
		private void LoadFontXML_kernpairs(XmlNodeList xnl)
		{
			foreach (XmlNode xn in xnl)
			{
				if (xn.Name == "kernpair")
				{
					string strLeft = GetXMLAttribute(xn, "left");
					string strRight = GetXMLAttribute(xn, "right");
					string strAdjust = GetXMLAttribute(xn, "adjust");

					char chLeft = strLeft[0];
					char chRight = strRight[0];

					// create a kern dict for the left char if needed
					if (!_dictKern.ContainsKey(chLeft))
						_dictKern[chLeft] = new Dictionary<char,sbyte>();

					// add the right char to the left char's kern dict
					Dictionary<char, sbyte> kern2 = _dictKern[chLeft];
					kern2[chRight] = SByte.Parse(strAdjust);
				}
			}
		}

		/// <summary>
		/// Get the XML attribute value
		/// </summary>
		/// <param name="n">XML node</param>
		/// <param name="strAttr">Attribute name</param>
		/// <returns>Attribute value, or the empty string if the attribute doesn't exist</returns>
		private static string GetXMLAttribute(XmlNode n, string strAttr)
		{
			XmlAttribute attr = n.Attributes.GetNamedItem(strAttr) as XmlAttribute;
			if (attr != null)
				return attr.Value;
			return "";
		}

		#endregion
	}
}
