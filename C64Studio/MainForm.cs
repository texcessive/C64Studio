﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using C64Studio.IdleQueue;
using C64Studio.Types;

// 0.9f - added else for !ifdef macro
// 0.9b - fixed crash bug if opening project with modified active project


namespace C64Studio
{
  public partial class MainForm : Form, IMessageFilter 
  {
    [DllImport("USER32.DLL")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);



    //public Project          m_Project = null;
    private Project         m_CurrentProject = null;

    public Solution         m_Solution = null;

    public OutputDisplay    m_Output = new OutputDisplay();
    
    public SolutionExplorer  m_SolutionExplorer = new SolutionExplorer();

    public DebugRegisters   m_DebugRegisters = new DebugRegisters();
    public DebugWatch       m_DebugWatch = new DebugWatch();
    public DebugMemory      m_DebugMemory = new DebugMemory();
    public DebugBreakpoints m_DebugBreakpoints = new DebugBreakpoints();
    public CompileResult    m_CompileResult = new CompileResult();
    public CharsetEditor    m_CharsetEditor = null;
    public CharsetScreenEditor  m_CharScreenEditor = null;
    public GraphicScreenEditor  m_GraphicScreenEditor = null;
    public SpriteEditor     m_SpriteEditor = null;
    public MapEditor        m_MapEditor = null;
    public Calculator       m_Calculator = null;
    public PetSCIITable     m_PetSCIITable = null;
    public Outline          m_Outline = new Outline();
    public Help             m_Help = new Help();
    public FormFindReplace  m_FindReplace = new FormFindReplace();
    
    public SearchResults    m_SearchResults = new SearchResults();
    public Perspective      m_ActivePerspective = Perspective.DEBUG;

    public System.Diagnostics.Process CompilerProcess = null;
    private System.Diagnostics.Process  m_ExternalProcess = null;

    public System.Diagnostics.Process RunProcess = null;

    public StudioCore       StudioCore = new StudioCore();

    private List<Tasks.Task> m_Tasks = new List<C64Studio.Tasks.Task>();
    private Tasks.Task      m_CurrentTask = null;

    private System.DateTime m_LastReceivedOutputTime;

    private bool            m_ChangingToolWindows = false;
    private bool            m_LoadingProject = false;

    private BaseDocument    m_ActiveSource = null;
    private ToolInfo        m_CurrentActiveTool = null;
    public SortedDictionary<string, Types.Palette> Palettes = new SortedDictionary<string, C64Studio.Types.Palette>();

    private static MainForm s_MainForm = null;
    private List<IdleRequest>   IdleQueue = new List<IdleRequest>();


    public delegate void ApplicationEventHandler( Types.ApplicationEvent Event );

    public event ApplicationEventHandler  ApplicationEvent;

    private GR.Collections.Set<BaseDocument>            m_ExternallyChangedDocuments = new GR.Collections.Set<BaseDocument>();

    public System.Drawing.Text.PrivateFontCollection    m_FontC64 = new System.Drawing.Text.PrivateFontCollection();



    delegate void AddToOutputAndShowCallback( string Text );
    delegate void SetGUIForWaitOnExternalToolCallback( bool Wait );
    delegate void SetDebuggerValuesCallback( string[] RegisterValues );
    delegate void StartDebugAtCallback( DocumentInfo DocumentToDebug, DocumentInfo DocumentToRun, int DebugAddress );
    delegate void ParameterLessCallback();
    delegate void UpdateWatchInfoCallback( RemoteDebugger.RequestData Request, GR.Memory.ByteBuffer Data );
    delegate bool ParseFileCallback( Parser.ParserBase Parser, DocumentInfo Document, ProjectConfig Configuration );
    delegate void DocCallback( BaseDocument Document );
    delegate void DocumentEventHandlerCallback( BaseDocument.DocEvent Event );
    delegate void NotifyAllDocumentsCallback( bool CanToggleBreakpoints );


    /*
    // TASM decompile
    public Systems.CPUSystem            s_Processor = Systems.CPUSystem.Create6510System();
    public bool s_NextLabelIsSingle = false;



    private string MnemonicToString( Types.ASM.Opcode opcode, GR.Memory.ByteBuffer Data, ref int CodePos )
    {
      string output = opcode.Mnemonic.ToLower();

      string  addressText = "";

      ++CodePos;
      if ( opcode.NumOperands > 0 )
      {
        bool dummy = false;
        addressText = DecompileNextValue( Data, ref CodePos, ref dummy );
      }
      switch ( opcode.Addressing )
      {
        case Types.ASM.Opcode.AddressingType.IMPLICIT:
          break;
        case Types.ASM.Opcode.AddressingType.ABSOLUTE:
          output += " " + addressText;
          break;
        case Types.ASM.Opcode.AddressingType.ABSOLUTE_X:
          output += " " + addressText + ", x";
          break;
        case Types.ASM.Opcode.AddressingType.ABSOLUTE_Y:
          output += " " + addressText + ", y";
          break;
        case Types.ASM.Opcode.AddressingType.IMMEDIATE:
          output += " #" + addressText;
          break;
        case Types.ASM.Opcode.AddressingType.INDIRECT:
          output += " ( " + addressText + " )";
          break;
        case Types.ASM.Opcode.AddressingType.INDIRECT_X:
          output += " ( " + addressText + ", x)";
          break;
        case Types.ASM.Opcode.AddressingType.INDIRECT_Y:
          output += " ( " + addressText + " ), y";
          break;
        case Types.ASM.Opcode.AddressingType.RELATIVE:
          {
            // int delta = value - lineInfo.AddressStart - 2;

            output += " " + addressText;
            //output += " (" + delta.ToString( "X2" ) + ")";
          }
          break;
        case Types.ASM.Opcode.AddressingType.ZEROPAGE:
          output += " " + addressText;
          break;
        case Types.ASM.Opcode.AddressingType.ZEROPAGE_X:
          output += " " + addressText + ", x";
          break;
        case Types.ASM.Opcode.AddressingType.ZEROPAGE_Y:
          output += " " + addressText + ", y";
          break;
      }
      return output;
    }



    public string DecompileNextValue( GR.Memory.ByteBuffer Data, ref int Pos, ref bool LineComplete )
    {
      LineComplete = false;

      byte valueToCheck = Data.ByteAt( Pos );

      switch ( valueToCheck )
      {
        case 0x28:
          // 28 = 1 byte value hex
          Pos += 2;
          return "$" + Data.ByteAt( Pos - 1 ).ToString( "X2" );
        case 0x2A:
          // 2A = 1 byte value decimal
          Pos += 2;
          return Data.ByteAt( Pos - 1 ).ToString( "D" );
        case 0x2B:
          // 2B = 2 byte value decimal
          Pos += 3;
          return Data.UInt16At( Pos - 2 ).ToString( "D" );
        case 0x2C:
          // 2C = 1 byte value binary
          Pos += 2;
          return "%" + Convert.ToString( Data.ByteAt( Pos - 1 ), 2 );
        case 0x29:
          // 29 = 2 byte value
          Pos += 3;
          return "$" + Data.UInt16At( Pos - 2 ).ToString( "X4" );
        case 0x38:
          // 38 = replacement label + index
          {
            byte    labelIndex = Data.ByteAt( Pos + 1 );

            Pos += 2;
            return "Label_No_" + labelIndex.ToString();
          }
        case 0x44:
          {
            // 44 = high byte of followup
            ++Pos;

            return ">" + DecompileNextValue( Data, ref Pos, ref LineComplete );
          }
        case 0x45:
          {
            // 45 = low byte of followup
            ++Pos;
            return "<" + DecompileNextValue( Data, ref Pos, ref LineComplete );
          }
      }
      ++Pos;
      return "?";
    }




    public string DecompileNext( GR.Memory.ByteBuffer Data, ref int Pos, ref bool LineComplete )
    {
      LineComplete = false;

      byte valueToCheck = Data.ByteAt( Pos );

      switch ( valueToCheck )
      {
        case 0x89:
          // 89 = full comment
          {
            string comment = "";

            ++Pos;

            while ( Pos < Data.Length )
            {
              byte  value = Data.ByteAt( Pos );

              if ( ( value >= 0x20 )
              &&   ( value <= 0x7f ) )
              {
                comment += (char)value;
                ++Pos;
              }
              else
              {
                break;
              }
            }
            LineComplete = true;
            return ";" + comment;
          }
        case 0x91:
        case 0x93:
        case 0x95:
        case 0x94:
        case 0x9A:
          if ( valueToCheck == 0x91 )
          {
            s_NextLabelIsSingle = true;
          }
          else
          {
            s_NextLabelIsSingle = false;
          }

          // 9A = end of line comment with different ending
          {
            string comment = "";

            ++Pos;

            while ( Pos < Data.Length )
            {
              byte  value = Data.ByteAt( Pos );

              if ( ( value != 0x30 )
              &&   ( value >= 0x20 )
              &&   ( value <= 0x7f ) )
              {
                comment += (char)value;
                ++Pos;
              }
              else
              {
                break;
              }
            }
            LineComplete = true;
            return comment;
          }
        case 0x92:
        case 0x9B:
          // 9A/9B = end of line comment
          {
            string comment = "";

            ++Pos;

            while ( Pos < Data.Length )
            {
              byte  value = Data.ByteAt( Pos );

              if ( ( value >= 0x20 )
              &&   ( value <= 0x7f ) )
              {
                comment += (char)value;
                ++Pos;
              }
              else
              {
                break;
              }
            }
            LineComplete = true;
            return comment;
          }
        case 0:
          // 00 = line done
          LineComplete = true;
          return "";
        case 0x03:
          // 03 = binary data
          {
            Pos += 1;

            string result = "";
            bool firstFollowup = true;

            while ( Pos < Data.Length )
            {
              byte    nextOpcode = Data.ByteAt( Pos );
              if ( ( nextOpcode == 0x28 )
              || ( nextOpcode == 0x29 )
              || ( nextOpcode == 0x2A ) )
              {
                if ( firstFollowup )
                {
                  firstFollowup = false;
                  result += " .byte " + DecompileNextValue( Data, ref Pos, ref LineComplete );
                }
                else
                {
                  result += "," + DecompileNextValue( Data, ref Pos, ref LineComplete );
                }
              }
              else
              {
                break;
              }
            }
            LineComplete = true;
            return result;
          }
        case 0x30:
          // 30 = label + index
          {
            byte    labelIndex = Data.ByteAt( Pos + 1 );
            byte    followUpDataType = Data.ByteAt( Pos + 2 );

            Pos += 2;
            LineComplete = false;

            if ( s_NextLabelIsSingle )
            {
              s_NextLabelIsSingle = false;

              LineComplete = true;
              return "Label_No_" + labelIndex.ToString();
            }

            string result = "Label_No_" + labelIndex.ToString() + " " + DecompileNext( Data, ref Pos, ref LineComplete );
            LineComplete = true;
            return result;
          }
      }
      if ( s_Processor.OpcodeByValue.ContainsKey( valueToCheck ) )
      {
        var opCode = s_Processor.OpcodeByValue[valueToCheck];

        // TODO - Labels!
        string result = MnemonicToString( opCode, Data, ref Pos );

        // comment appended?
        if ( ( Data.ByteAt( Pos ) == 0x91 )
        ||   ( Data.ByteAt( Pos ) == 0x92 )
        ||   ( Data.ByteAt( Pos ) == 0x93 )
        ||   ( Data.ByteAt( Pos ) == 0x94 )
        ||   ( Data.ByteAt( Pos ) == 0x95 )
        ||   ( Data.ByteAt( Pos ) == 0x9a )
        ||   ( Data.ByteAt( Pos ) == 0x9b ) )
        {
          result += " ;" + DecompileNext( Data, ref Pos, ref LineComplete );
        }

        LineComplete = true;

        return result;
      }
      return "?";
    }*/



    public MainForm( string[] args )
    {
      s_MainForm = this;

      //m_FontC64.AddFontFile( @"D:\privat\projekte\C64Studio\C64Studio\C64_Pro_Mono_v1.0-STYLE.ttf" );

      try
      {
        string  basePath = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
        if ( basePath.ToUpper().StartsWith( "FILE:///" ) )
        {
          basePath = basePath.Substring( 8 );
        }
        string  fontPath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName( basePath ), @"C64_Pro_Mono_v1.0-STYLE.ttf" );


        m_FontC64.AddFontFile( fontPath ); // @"C64_Pro_Mono_v1.0-STYLE.ttf" );
      }
      catch ( Exception ex )
      {
        MessageBox.Show( "C64Studio can't find the C64 true type font file C64_Pro_Mono_v1.0-STYLE.ttf.\r\nMake sure it's in the path of C64Studio.exe.\r\n\r\n" + ex.Message, "Can't load font" );
        return;
      }

      /*
      // TASM - decompile from project file
      GR.Memory.ByteBuffer      data = GR.IO.File.ReadAllBytes( @"D:\privat\projekte\c64\TurboAssembler\rasteri_src.prg" );

      int       pos = 256;
      string    currentLine = "";
      string    result = "";

      s_NextLabelIsSingle = false;

      while ( pos < (int)data.Length )
      {
        bool lineComplete = false;
        string  decompiled = DecompileNext( data, ref pos, ref lineComplete );
        if ( decompiled.Length == 0 )
        {
          break;
        }


        //currentLine += decompiled;
        currentLine = decompiled + currentLine;

        if ( lineComplete )
        {
          result = currentLine + "\r\n" + result;
          currentLine = "";
        }
      }

      Debug.Log( result );
      return;*/


      // init custom renderer
      //ToolStripManager.Renderer = new CustomRenderer.CustomToolStripRenderer();
      //ToolStripManager.Renderer = new C64Studio.CustomRenderer.LightToolStripRenderer();

      InitializeComponent();

      Application.Idle += new EventHandler( Application_Idle );

      panelMain.ShowDocumentIcon = true;

#if DEBUG
      debugToolStripMenuItem.Visible = true;
#else
      debugToolStripMenuItem.Visible = false;
#endif


      statusProgress.Visible = false;

      StudioCore.MainForm = this;
      StudioCore.Settings.PanelMain = panelMain;
      StudioCore.Settings.Main = this;

      Parser.BasicFileParser.KeyMap = StudioCore.Settings.BASICKeyMap;

      Types.Palette defaultPalette = new C64Studio.Types.Palette();
      defaultPalette.Name = "C64Studio";

      Palettes.Add( defaultPalette.Name, defaultPalette );

      StudioCore.Debugging.Debugger = new RemoteDebugger( StudioCore );

      Application.AddMessageFilter( this );

      m_CharsetEditor = new CharsetEditor( StudioCore );
      m_SpriteEditor = new SpriteEditor( StudioCore );
      m_GraphicScreenEditor = new GraphicScreenEditor( StudioCore );
      m_CharScreenEditor = new CharsetScreenEditor( StudioCore );
      m_PetSCIITable = new PetSCIITable( StudioCore );
      m_Calculator = new Calculator();
      m_MapEditor = new MapEditor( StudioCore );

      m_CharsetEditor.SetInternal();
      m_SpriteEditor.SetInternal();
      m_GraphicScreenEditor.SetInternal();
      m_CharScreenEditor.SetInternal();
      m_MapEditor.SetInternal();

      // build default panes
      AddToolWindow( ToolWindowType.OUTLINE, m_Outline, DockState.DockRight, outlineToolStripMenuItem, true, true );
      AddToolWindow( ToolWindowType.SOLUTION_EXPLORER, m_SolutionExplorer, DockState.DockRight, projectExplorerToolStripMenuItem, true, true );
      AddToolWindow( ToolWindowType.OUTPUT, m_Output, DockState.DockBottom, outputToolStripMenuItem, true, true );
      AddToolWindow( ToolWindowType.COMPILE_RESULT, m_CompileResult, DockState.DockBottom, compileResulttoolStripMenuItem, true, true );
      AddToolWindow( ToolWindowType.DEBUG_REGISTERS, m_DebugRegisters, DockState.DockRight, debugRegistersToolStripMenuItem, false, true );
      AddToolWindow( ToolWindowType.DEBUG_WATCH, m_DebugWatch, DockState.DockBottom, debugWatchToolStripMenuItem, false, true );
      AddToolWindow( ToolWindowType.DEBUG_MEMORY, m_DebugMemory, DockState.DockRight, debugMemoryToolStripMenuItem, false, true );
      m_DebugMemory.ViewScrolled += new DebugMemory.DebugMemoryEventCallback( m_DebugMemory_ViewScrolled );
      AddToolWindow( ToolWindowType.DEBUG_BREAKPOINTS, m_DebugBreakpoints, DockState.DockRight, breakpointsToolStripMenuItem, false, true );
      m_DebugBreakpoints.DocumentEvent += new BaseDocument.DocumentEventHandler( Document_DocumentEvent );
      AddToolWindow( ToolWindowType.CHARSET_EDITOR, m_CharsetEditor, DockState.Document, charsetEditorToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.SPRITE_EDITOR, m_SpriteEditor, DockState.Document, spriteEditorToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.CHAR_SCREEN_EDITOR, m_CharScreenEditor, DockState.Document, charScreenEditorToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.GRAPHIC_SCREEN_EDITOR, m_GraphicScreenEditor, DockState.Document, graphicScreenEditorToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.MAP_EDITOR, m_MapEditor, DockState.Document, mapEditorToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.PETSCII_TABLE, m_PetSCIITable, DockState.Float, petSCIITableToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.CALCULATOR, m_Calculator, DockState.DockRight, calculatorToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.HELP, m_Help, DockState.Document, helpToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.FIND_REPLACE, m_FindReplace, DockState.Float, searchReplaceToolStripMenuItem, false, false );
      AddToolWindow( ToolWindowType.SEARCH_RESULTS, m_SearchResults, DockState.DockBottom, searchResultsToolStripMenuItem, false, false );

      StudioCore.Debugging.Debugger.DocumentEvent += new BaseDocument.DocumentEventHandler( Document_DocumentEvent );

      StudioCore.Settings.GenericTools["Outline"] = m_Outline;
      StudioCore.Settings.GenericTools["SolutionExplorer"] = m_SolutionExplorer;
      StudioCore.Settings.GenericTools["Output"] = m_Output;
      StudioCore.Settings.GenericTools["CompileResult"] = m_CompileResult;
      StudioCore.Settings.GenericTools["DebugRegisters"] = m_DebugRegisters;
      StudioCore.Settings.GenericTools["DebugWatch"] = m_DebugWatch;
      StudioCore.Settings.GenericTools["DebugMemory"] = m_DebugMemory;
      StudioCore.Settings.GenericTools["DebugBreakpoints"] = m_DebugBreakpoints;
      StudioCore.Settings.GenericTools["CharsetEditor"] = m_CharsetEditor;
      StudioCore.Settings.GenericTools["SpriteEditor"] = m_SpriteEditor;
      StudioCore.Settings.GenericTools["CharScreenEditor"] = m_CharScreenEditor;
      StudioCore.Settings.GenericTools["GraphicScreenEditor"] = m_GraphicScreenEditor;
      StudioCore.Settings.GenericTools["MapEditor"] = m_MapEditor;
      StudioCore.Settings.GenericTools["PetSCIITable"] = m_PetSCIITable;
      StudioCore.Settings.GenericTools["Calculator"] = m_Calculator;
      StudioCore.Settings.GenericTools["Help"] = m_Help;
      StudioCore.Settings.GenericTools["FindReplace"] = m_FindReplace;
      StudioCore.Settings.GenericTools["SearchResults"] = m_SearchResults;

      m_DebugMemory.hexView.TextFont = new System.Drawing.Font( m_FontC64.Families[0], 9, System.Drawing.GraphicsUnit.Pixel );
      m_DebugMemory.hexView.ByteCharConverter = new C64Studio.Converter.PETSCIIToCharConverter();

      if ( !LoadSettings() )
      {
        if ( StudioCore.Settings.BASICKeyMap.DefaultKeymaps.ContainsKey( (uint)System.Windows.Forms.InputLanguage.CurrentInputLanguage.Culture.LCID ) )
        {
          StudioCore.Settings.BASICKeyMap.Keymap = StudioCore.Settings.BASICKeyMap.DefaultKeymaps[(uint)System.Windows.Forms.InputLanguage.CurrentInputLanguage.Culture.LCID];
        }
        else
        {
          // default to english
          StudioCore.Settings.BASICKeyMap.Keymap = StudioCore.Settings.BASICKeyMap.DefaultKeymaps[9];
        }

        StudioCore.Settings.SetDefaultKeyBinding();
        StudioCore.Settings.SetDefaultColors();
      }
      else
      {
        foreach ( LayoutInfo layout in StudioCore.Settings.ToolLayout.Values )
        {
          layout.RestoreLayout();
        }
        if ( StudioCore.Settings.BASICKeyMap.Keymap.Count == 0 )
        {
          if ( StudioCore.Settings.BASICKeyMap.DefaultKeymaps.ContainsKey( (uint)System.Windows.Forms.InputLanguage.CurrentInputLanguage.Culture.LCID ) )
          {
            StudioCore.Settings.BASICKeyMap.Keymap = StudioCore.Settings.BASICKeyMap.DefaultKeymaps[(uint)System.Windows.Forms.InputLanguage.CurrentInputLanguage.Culture.LCID];
          }
          else
          {
            // default to english
            StudioCore.Settings.BASICKeyMap.Keymap = StudioCore.Settings.BASICKeyMap.DefaultKeymaps[9];
          }
        }
      }
      StudioCore.Settings.SanitizeSettings();
      StudioCore.Compiling.ParserBasic.Settings.StripSpaces = StudioCore.Settings.BASICStripSpaces;
      m_Outline.checkShowLocalLabels.Image = StudioCore.Settings.OutlineShowLocalLabels ? C64Studio.Properties.Resources.flag_green_on.ToBitmap() : C64Studio.Properties.Resources.flag_green_off.ToBitmap();
      m_Outline.checkShowShortCutLabels.Image = StudioCore.Settings.OutlineShowShortCutLabels ? C64Studio.Properties.Resources.flag_blue_on.ToBitmap() : C64Studio.Properties.Resources.flag_blue_off.ToBitmap();

      EmulatorListUpdated();

      if ( StudioCore.Settings.TrueDriveEnabled )
      {
        mainToolToggleTrueDrive.Image = Properties.Resources.toolbar_truedrive_enabled;
      }
      else
      {
        mainToolToggleTrueDrive.Image = Properties.Resources.toolbar_truedrive_disabled;
      }

      // place all toolbars
      SetToolPerspective( Perspective.EDIT );

      if ( StudioCore.Settings.MainWindowPlacement != "" )
      {
        GR.Forms.WindowStateManager.GeometryFromString( StudioCore.Settings.MainWindowPlacement, this );
      }

      foreach ( Types.ColorableElement syntax in Enum.GetValues( typeof( Types.ColorableElement ) ) )
      {
        if ( StudioCore.Settings.SyntaxColoring[syntax] == null )
        {
          switch ( syntax )
          {
            case C64Studio.Types.ColorableElement.NONE:
            case C64Studio.Types.ColorableElement.LABEL:
              // dark red
              StudioCore.Settings.SyntaxColoring[syntax] = new C64Studio.Types.ColorSetting( GR.EnumHelper.GetDescription( syntax ) );
              StudioCore.Settings.SyntaxColoring[syntax].FGColor = 0xff800000;
              break;
            case C64Studio.Types.ColorableElement.CURRENT_DEBUG_LINE:
              // yellow background
              StudioCore.Settings.SyntaxColoring[syntax] = new C64Studio.Types.ColorSetting( GR.EnumHelper.GetDescription( syntax ) );
              StudioCore.Settings.SyntaxColoring[syntax].BGColor = 0xffffff00;
              StudioCore.Settings.SyntaxColoring[syntax].BGColorAuto = false;
              break;
            case C64Studio.Types.ColorableElement.LITERAL_NUMBER:
            case C64Studio.Types.ColorableElement.OPERATOR:
            case C64Studio.Types.ColorableElement.LITERAL_STRING:
              // blue on background
              StudioCore.Settings.SyntaxColoring[syntax] = new C64Studio.Types.ColorSetting( GR.EnumHelper.GetDescription( syntax ) );
              StudioCore.Settings.SyntaxColoring[syntax].FGColor = 0xff0000ff;
              break;
            case C64Studio.Types.ColorableElement.COMMENT:
              // dark green on background
              StudioCore.Settings.SyntaxColoring[syntax] = new C64Studio.Types.ColorSetting( GR.EnumHelper.GetDescription( syntax ) );
              StudioCore.Settings.SyntaxColoring[syntax].FGColor = 0xff008000;
              break;
            case Types.ColorableElement.ERROR_UNDERLINE:
              // only forecolor needed, red
              StudioCore.Settings.SyntaxColoring[syntax] = new Types.ColorSetting( GR.EnumHelper.GetDescription( syntax ) );
              StudioCore.Settings.SyntaxColoring[syntax].FGColor = 0xffff0000;
              break;
          }
        }
        if ( StudioCore.Settings.SyntaxColoring[syntax] == null )
        {
          StudioCore.Settings.SyntaxColoring[syntax] = new C64Studio.Types.ColorSetting( GR.EnumHelper.GetDescription( syntax ) );
        }
      }
      m_FindReplace.Fill( StudioCore.Settings );

      panelMain.ActiveContentChanged += new EventHandler( panelMain_ActiveContentChanged );
      panelMain.ActiveDocumentChanged += new EventHandler( panelMain_ActiveDocumentChanged );
      StudioCore.Settings.ReadMRU();
      UpdateMenuMRU();
      UpdateUndoSettings();

      mainTools.Visible = StudioCore.Settings.ToolbarActiveMain;
      debugTools.Visible = StudioCore.Settings.ToolbarActiveDebugger;

      SetGUIForWaitOnExternalTool( false );

      projectToolStripMenuItem.Visible = false;

      panelMain.AllowDrop = true;
      panelMain.DragEnter += new DragEventHandler( MainForm_DragEnter );
      panelMain.DragDrop += new DragEventHandler( MainForm_DragDrop );

      //DumpPanes( panelMain, "" );

      ApplicationEvent += new ApplicationEventHandler( MainForm_ApplicationEvent );

      if ( args.Length > 0 )
      {
        OpenFile( args[0] );
      }
      else if ( StudioCore.Settings.AutoOpenLastSolution )
      {
        if ( ( !StudioCore.Settings.LastSolutionWasEmpty )
        &&   ( StudioCore.Settings.MRU.Count > 0 ) )
        {
          var idleRequest = new IdleRequest();
          idleRequest.OpenLastSolution = StudioCore.Settings.MRU[0];

          IdleQueue.Add( idleRequest );
        }
      }
    }



    void SetToolPerspective( Perspective NewPerspective )
    {
      if ( m_ActivePerspective == NewPerspective )
      {
        return;
      }
      m_ActivePerspective = NewPerspective;

      foreach ( ToolWindow tool in StudioCore.Settings.Tools.Values )
      {
        if ( tool.Type == ToolWindowType.FIND_REPLACE )
        {
          // to not toggle visiblity of this
          continue;
        }
        if ( tool.Visible[NewPerspective] )
        {
          tool.Document.Show( panelMain );
          tool.MenuItem.Checked = true;
        }
        else
        {
          tool.Document.DockPanel = panelMain;
          tool.Document.DockState = DockState.Hidden;
          tool.MenuItem.Checked = false;
        }
      }
    }



    void Application_Idle( object sender, EventArgs e )
    {
      s_MainForm.OnIdle();
    }



    void OnIdle()
    {
      if ( IdleQueue.Count > 0 )
      {
        var request = IdleQueue[0];
        IdleQueue.RemoveAt( 0 );

        if ( request.DebugRequest != null )
        {
          StudioCore.Debugging.Debugger.QueueRequest( request.DebugRequest );
        }
        else if ( request.OpenLastSolution != null )
        {
          OpenFile( request.OpenLastSolution );
        }
      }
    }



    void EmulatorListUpdated()
    {
      ToolInfo  oldTool = null;
      if ( mainToolEmulator.SelectedItem != null )
      {
        oldTool = ( (GR.Generic.Tupel<string, ToolInfo>)mainToolEmulator.SelectedItem ).second;
      }

      mainToolEmulator.Items.Clear();
      foreach ( var tool in StudioCore.Settings.ToolInfos )
      {
        if ( tool.Type == ToolInfo.ToolType.EMULATOR )
        {
          int itemIndex = mainToolEmulator.Items.Add( new GR.Generic.Tupel<string,ToolInfo>( tool.Name, tool ) );
          if ( ( tool.Name.ToUpper() == StudioCore.Settings.EmulatorToRun )
          ||   ( oldTool == tool ) )
          {
            mainToolEmulator.SelectedIndex = itemIndex;
          }
        }
      }
      if ( ( mainToolEmulator.Items.Count != 0 )
      &&   ( mainToolEmulator.SelectedIndex == -1 ) )
      {
        mainToolEmulator.SelectedIndex = 0;
      }
    }



    public Types.Palette ActivePalette
    {
      get
      {
        // TODO - aus Palette-Liste
        return Types.ConstantData.Palette;
      }
    }



    public Project CurrentProject
    {
      get
      {
        return m_CurrentProject;
      }
    }



    public List<DocumentInfo> DocumentInfos
    {
      get
      {
        List<DocumentInfo>    list = new List<DocumentInfo>();

        if ( m_Solution != null )
        {
          foreach ( var project in m_Solution.Projects )
          {
            foreach ( var element in project.Elements )
            {
              list.Add( element.DocumentInfo );
            }
          }
        }
        foreach ( BaseDocument doc in panelMain.Documents )
        {
          if ( doc.DocumentInfo.Element == null )
          {
            list.Add( doc.DocumentInfo );
          }
        }
        return list;
      }
    }



    void DumpPanes( DockPanel Panel, string Indent )
    {
      Debug.Log( "Tools" );

      foreach ( ToolWindow tool in StudioCore.Settings.Tools.Values )
      {
        Debug.Log( tool.ToolDescription + " visible " + tool.Visible );
        Debug.Log( "-state " + tool.Document.DockState );
      }

      Debug.Log( Indent + "Panel " + Panel.Name );
      foreach ( IDockContent content in Panel.Documents )
      {
        Debug.Log( Indent + "-doc-" + content.ToString() );
      }
      foreach ( DockPane pane in Panel.Panes )
      {
        Debug.Log( Indent + "-pan-" + pane.DockState + "-" + pane.Name );
        foreach ( DockContent dock in pane.Contents )
        {
          Debug.Log( Indent + "--dock-" + dock.Name );
          if ( pane.DockState == DockState.Float )
          {
            Debug.Log( Indent + " pos " + dock.Location );
          }
        }
        //DumpPanes( pane.DockPanel, Indent + " " );
      }
      foreach ( FloatWindow wnd in Panel.FloatWindows )
      {
        Debug.Log( Indent + "-float-" + wnd.DockState + "-" + wnd.Name );
        Debug.Log( Indent + " pos " + wnd.Location + ", size " + wnd.Size );
        foreach ( DockPane pane in wnd.NestedPanes )
        {
          Debug.Log( Indent + "--pan-" + pane.DockState + "-" + pane.Name );
          Debug.Log( Indent + "-- pos " + pane.Location + ", size " + pane.Size );
          foreach ( DockContent dock in pane.Contents )
          {
            Debug.Log( Indent + "---dock-" + dock.Name );
            if ( pane.DockState == DockState.Float )
            {
              Debug.Log( Indent + "--- pos " + dock.Location );
            }
          }
        }
        //DumpPanes( pane.DockPanel, Indent + " " );
      }

      /*
      foreach ( DockWindow window in Panel.DockWindows )
      {
        Debug.Log( Indent + "-wnd-" + window.Name );
        //DumpPanes( window.NestedPanes, Indent + " " );
      }*/
    }



    void MainForm_ApplicationEvent( C64Studio.Types.ApplicationEvent Event )
    {
      switch ( Event.EventType )
      {
        case C64Studio.Types.ApplicationEvent.Type.EMULATOR_LIST_CHANGED:
          EmulatorListUpdated();
          break;
        case C64Studio.Types.ApplicationEvent.Type.DOCUMENT_ACTIVATED:
          if ( Event.Doc == null )
          {
            mainToolPrint.Enabled = false;
          }
          else
          {
            mainToolPrint.Enabled = Event.Doc.ContainsCode;
          }
          // current project changed?
          if ( Event.Project == null )
          {
            SetActiveProject( null );
          }
          else
          {
            SetActiveProject( Event.Project );
          }
          break;
        case C64Studio.Types.ApplicationEvent.Type.SOLUTION_OPENED:
          solutionToolStripMenuItem.Visible = true;
          solutionCloseToolStripMenuItem.Enabled = true;
          solutionSaveToolStripMenuItem1.Enabled = true;
          UpdateCaption();
          break;
        case C64Studio.Types.ApplicationEvent.Type.SOLUTION_CLOSED:
          solutionToolStripMenuItem.Visible = false;
          solutionCloseToolStripMenuItem.Enabled = false;
          solutionSaveToolStripMenuItem1.Enabled = false;

          m_Output.SetText( "" );
          m_CompileResult.ClearMessages();
          UpdateCaption();
          break;
        case C64Studio.Types.ApplicationEvent.Type.ACTIVE_PROJECT_CHANGED:
          m_DebugWatch.DebuggedProject          = m_CurrentProject;
          m_DebugRegisters.DebuggedProject      = m_CurrentProject;
          m_DebugMemory.DebuggedProject         = m_CurrentProject;
          m_DebugBreakpoints.DebuggedProject    = m_CurrentProject;
          UpdateCaption();
          break;
      }
    }



    void panelMain_ActiveDocumentChanged( object sender, EventArgs e )
    {
      var     docInfo = ActiveDocumentInfo;
      RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.DOCUMENT_ACTIVATED, docInfo ) );

      mainToolFind.Enabled = ( docInfo != null ) ? docInfo.Compilable : false;
      mainToolFindReplace.Enabled = mainToolFind.Enabled;
      mainToolCommentSelection.Enabled = ( docInfo != null ) ? docInfo.Compilable : false;
      mainToolUncommentSelection.Enabled = ( docInfo != null ) ? docInfo.Compilable : false;

      if ( ActiveDocument == null )
      {
        saveToolStripMenuItem.Enabled = false;
        saveAsToolStripMenuItem.Enabled = false;
        mainToolSave.Enabled = false;
        fileCloseToolStripMenuItem.Enabled = false;
      }
      else
      {
        saveToolStripMenuItem.Enabled = ActiveDocument.Modified;
        saveAsToolStripMenuItem.Enabled = true;
        mainToolSave.Enabled = ActiveDocument.Modified;
        fileCloseToolStripMenuItem.Enabled = true;
      }
    }



    public void RaiseApplicationEvent( Types.ApplicationEvent Event )
    {
      if ( ApplicationEvent != null )
      {
        ApplicationEvent( Event );
      }
    }



    public Types.StudioState AppState
    {
      get
      {
        return StudioCore.State;
      }
      set
      {
        StudioCore.State = value;

        bool    canToggleBreakpoints = false;

        if ( ( StudioCore.State == Types.StudioState.NORMAL )
        ||   ( StudioCore.State == Types.StudioState.DEBUGGING_BROKEN ) )
        {
          canToggleBreakpoints = true;
        }

        NotifyAllDocuments( canToggleBreakpoints );
      }
    }



    void NotifyAllDocuments( bool CanToggleBreakpoints )
    {
      if ( InvokeRequired )
      {
        Invoke( new NotifyAllDocumentsCallback( NotifyAllDocuments ), new object[] { CanToggleBreakpoints } );
        return;
      }

      foreach ( BaseDocument doc in panelMain.Contents )
      {
        doc.BreakpointToggleable = CanToggleBreakpoints;
      }
    }



    void panelMain_ActiveContentChanged( object sender, EventArgs e )
    {
      BaseDocument baseDoc = ActiveContent;
      if ( baseDoc == null )
      {
        mainToolUndo.Enabled = false;
        mainToolRedo.Enabled = false;
      }
      else
      {
        mainToolUndo.Enabled = baseDoc.UndoPossible;
        mainToolRedo.Enabled = baseDoc.RedoPossible;

        if ( baseDoc.DocumentInfo.ContainsCode )
        {
          if ( m_ActiveSource != baseDoc )
          {
            m_ActiveSource = baseDoc;
            //Debug.Log( "m_Outline.RefreshFromDocument after active content change" );
            m_Outline.RefreshFromDocument( baseDoc );
          }
        }
        saveToolStripMenuItem.Enabled = baseDoc.Modified;
        saveAsToolStripMenuItem.Enabled = true;
        mainToolSave.Enabled = baseDoc.Modified;
      }
    }



    void AddToolWindow( ToolWindowType Type, BaseDocument Document, DockState DockState, ToolStripMenuItem MenuItem, bool VisibleEdit, bool VisibleDebug )
    {
      ToolWindow tool = new ToolWindow();

      tool.Document = Document;
      tool.Document.Core = StudioCore;
      tool.Document.ShowHint = DockState;
      tool.Document.FormClosed += new FormClosedEventHandler( Document_FormClosed );
      tool.Document.VisibleChanged += new EventHandler( Document_VisibleChanged );
      tool.Document.HideOnClose = true;
      tool.MenuItem = MenuItem;
      tool.MenuItem.CheckOnClick = true;
      tool.MenuItem.CheckedChanged += new EventHandler( MenuItem_CheckedChanged );
      tool.Visible[Perspective.EDIT] = VisibleEdit;
      tool.Visible[Perspective.DEBUG] = VisibleDebug;
      tool.ToolDescription = GR.EnumHelper.GetDescription( Type );
      tool.Type = Type;
      /*
      if ( Visible )
      {
        tool.Document.Show( panelMain );
        tool.MenuItem.Checked = true;
      }
      else
      {
        //tool.Document.Show( panelMain );
        tool.Document.DockPanel = panelMain;
        tool.Document.DockState = DockState.Hidden;
        tool.MenuItem.Checked = false;
      }
       */
      LayoutInfo layout = null;
      if ( StudioCore.Settings.ToolLayout.ContainsKey( MenuItem.Text ) )
      {
        layout = StudioCore.Settings.ToolLayout[MenuItem.Text];
      }
      else
      {
        layout = new LayoutInfo();
        StudioCore.Settings.ToolLayout.Add( MenuItem.Text, layout );
      }
      layout.Name     = MenuItem.Text;
      layout.Document = Document;
      layout.StoreLayout();
      StudioCore.Settings.Tools[Type] = tool;
    }



    void Document_VisibleChanged( object sender, EventArgs e )
    {
      BaseDocument baseDoc = (BaseDocument)sender;
      if ( !baseDoc.IsHidden )
      {
        return;
      }
      if ( m_ChangingToolWindows )
      {
        return;
      }
      m_ChangingToolWindows = true;
      foreach ( ToolWindow tool in StudioCore.Settings.Tools.Values )
      {
        if ( tool.Document == sender )
        {
          tool.MenuItem.Checked = !tool.Document.IsHidden;
          tool.Visible[m_ActivePerspective] = !tool.Document.IsHidden;
          break;
        }
      }
      m_ChangingToolWindows = false;
    }



    void Document_FormClosed( object sender, FormClosedEventArgs e )
    {
      if ( m_ChangingToolWindows )
      {
        return;
      }
      m_ChangingToolWindows = true;
      foreach ( ToolWindow tool in StudioCore.Settings.Tools.Values )
      {
        if ( tool.Document == sender )
        {
          tool.MenuItem.Checked = false;
          tool.Visible[m_ActivePerspective] = false;
          break;
        }
      }
      m_ChangingToolWindows = false;
    }



    void MenuItem_CheckedChanged( object sender, EventArgs e )
    {
      if ( m_ChangingToolWindows )
      {
        return;
      }
      m_ChangingToolWindows = true;
      foreach ( ToolWindow tool in StudioCore.Settings.Tools.Values )
      {
        if ( tool.MenuItem == sender )
        {
          tool.Visible[m_ActivePerspective] = tool.MenuItem.Checked;
          if ( tool.MenuItem.Checked )
          {
            tool.Document.Show( panelMain );
          }
          else
          {
            tool.Document.Hide();
          }
          m_ChangingToolWindows = false;
          return;
        }
      }
      m_ChangingToolWindows = false;
    }



    void m_DebugMemory_ViewScrolled( object Sender, DebugMemory.DebugMemoryEvent Event )
    {
      if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      {
        // request new memory
        RemoteDebugger.RequestData requestRefresh = new RemoteDebugger.RequestData( RemoteDebugger.Request.REFRESH_MEMORY, m_DebugMemory.MemoryStart, m_DebugMemory.MemorySize );
        requestRefresh.Reason = RemoteDebugger.RequestReason.MEMORY_FETCH;

        IdleRequest debugFetch = new IdleRequest();
        debugFetch.DebugRequest = requestRefresh;

        IdleQueue.Add( debugFetch );
      }
    }


    public void UpdateMenuMRU()
    {
      int indexTop = fileToolStripMenuItem.DropDownItems.IndexOf( toolStripSeparatorAboveMRU );
      int index = fileToolStripMenuItem.DropDownItems.IndexOf( toolStripSeparatorBelowMRU );
      while ( indexTop + 1 < index )
      {
        fileToolStripMenuItem.DropDownItems[index - 1].Click -= menuMRUItem_Click;
        fileToolStripMenuItem.DropDownItems.RemoveAt( index - 1 );
        index = fileToolStripMenuItem.DropDownItems.IndexOf( toolStripSeparatorBelowMRU );
      }
      foreach ( string entry in StudioCore.Settings.MRU )
      {
        ToolStripMenuItem menuItem = new ToolStripMenuItem( entry );
        menuItem.Click += new EventHandler( menuMRUItem_Click );
        fileToolStripMenuItem.DropDownItems.Insert( index, menuItem );
        ++index;
      }
    }



    bool CloseAllProjects()
    {
      if ( m_Solution == null )
      {
        return true;
      }
      SaveSolution();
      foreach ( Project project in m_Solution.Projects )
      {
        if ( !SaveProject( project ) )
        {
          return false;
        }
      }
      while ( m_Solution.Projects.Count > 0 )
      {
        if ( !CloseProject( m_Solution.Projects[0] ) )
        {
          return false;
        }
      }
      m_SolutionExplorer.treeProject.Nodes.Clear();
      mainToolConfig.Items.Clear();
      m_CurrentProject = null;
      projectToolStripMenuItem.Visible = false;
      StudioCore.Debugging.BreakPoints.Clear();
      //CloseAllDocuments();
      return true;
    }



    void menuMRUItem_Click( object sender, EventArgs e )
    {
      CloseSolution();

      string extension = System.IO.Path.GetExtension( sender.ToString() ).ToUpper();

      if ( extension == ".C64" )
      {
        if ( OpenProject( sender.ToString() ) == null )
        {
          StudioCore.Settings.RemoveFromMRU( sender.ToString(), this );
        }
      }
      else if ( extension == ".S64" )
      {
        if ( !OpenSolution( sender.ToString() ) )
        {
          StudioCore.Settings.RemoveFromMRU( sender.ToString(), this );
        }
      }
    }


    // P/Invoke declarations     
    [DllImport("user32.dll")]     
    private static extern IntPtr WindowFromPoint(Point pt);     
    [DllImport("user32.dll")]     
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    [DllImport( "user32.dll" )]
    static extern bool InvalidateRect( IntPtr hWnd, IntPtr lpRect, bool bErase );



    public bool PreFilterMessage( ref Message m ) 
    {       
      // hack for scintilla to work with Katmouse
      if ( m.Msg == 0x20a ) 
      {         
        // WM_MOUSEWHEEL, find the control at screen position m.LParam         
        Point pos = new Point( m.LParam.ToInt32() & 0xffff, m.LParam.ToInt32() >> 16 );         
        IntPtr hWnd = WindowFromPoint( pos );         
        if ( hWnd != IntPtr.Zero && hWnd != m.HWnd )//&& Control.FromHandle( hWnd ) != null ) 
        {           
          SendMessage( hWnd, m.Msg, m.WParam, m.LParam );           
          return true;         
        }       
      }       
      return false;     
    } 



    private void exitToolStripMenuItem_Click( object sender, EventArgs e )
    {
      Close();
    }



    private void SaveAllDocuments()
    {
      foreach ( IDockContent dockContent in panelMain.Contents )
      {
        BaseDocument baseDoc = (BaseDocument)dockContent;

        if ( baseDoc.Modified )
        {
          if ( !baseDoc.IsInternal )
          {
            baseDoc.Save();
          }
        }
      }
      saveAllToolStripMenuItem.Enabled = false;
      saveToolStripMenuItem.Enabled = false;
      saveAsToolStripMenuItem.Enabled = true;
    }



    public DocumentInfo ActiveDocumentInfo
    {
      get
      {
        var baseDoc = (BaseDocument)panelMain.ActiveDocument;
        if ( baseDoc == null )
        {
          return null;
        }
        return baseDoc.DocumentInfo;
      }
      set
      {
        if ( value.BaseDoc != null )
        {
          value.BaseDoc.Select();
        }
      }
    }



    public BaseDocument ActiveDocument
    {
      get
      {
        /*
        // if active messes up compile
        if ( panelMain.ActiveContent != null )
        {
          return (BaseDocument)panelMain.ActiveContent;
        }*/
        return (BaseDocument)panelMain.ActiveDocument;
      }
      set
      {
        value.Select();
      }
    }



    public BaseDocument ActiveContent
    {
      get
      {
        return (BaseDocument)panelMain.ActiveContent;
      }
      set
      {
        value.Select();
      }
    }



    public ProjectElement ActiveElement
    {
      get
      {
        BaseDocument doc = (BaseDocument)panelMain.ActiveContent;
        if ( doc == null )
        {
          return null;
        }
        return doc.DocumentInfo.Element;
      }
    }



    private Parser.ParserBase DetermineParser( DocumentInfo Doc )
    {
      if ( Doc.Type == ProjectElement.ElementType.ASM_SOURCE )
      {
        return StudioCore.Compiling.ParserASM;
      }
      if ( Doc.Type == ProjectElement.ElementType.BASIC_SOURCE )
      {
        return StudioCore.Compiling.ParserBasic;
      }
      return null;
    }



    private ToolInfo DetermineTool( DocumentInfo Document, bool Run )
    {
      foreach ( ToolInfo tool in StudioCore.Settings.ToolInfos )
      {
        if ( ( Run )
        &&   ( tool.Type == ToolInfo.ToolType.EMULATOR )
        &&   ( tool.Name.ToUpper() == StudioCore.Settings.EmulatorToRun ) )
        {
          //AddToOutput( "Determined tool to run = " + tool.Name );
          return tool;
        }
      }

      // fallback
      foreach ( ToolInfo tool in StudioCore.Settings.ToolInfos )
      {
        if ( ( Run )
        &&   ( tool.Type == ToolInfo.ToolType.EMULATOR ) )
        {
          //AddToOutput( "fallback emulator = " + tool.Name );
          return tool;
        }
        if ( ( !Run )
        &&   ( tool.Type == ToolInfo.ToolType.ASSEMBLER ) )
        {
          return tool;
        }
      }
      return null;
    }



    public string FillParameters( string Mask, DocumentInfo Document, bool FillForRunning, out bool Error )
    {
      Error = false;
      if ( Document == null )
      {
        return Mask;
      }
      string    fullDocPath = Document.FullPath;
      string    result = Mask.Replace( "$(Filename)", fullDocPath );
      result = result.Replace( "$(FilenameWithoutExtension)", System.IO.Path.GetFileNameWithoutExtension( fullDocPath ) );
      result = result.Replace( "$(FilePath)", GR.Path.RemoveFileSpec( fullDocPath ) );

      string targetFilename = "";
      if ( Document.Element == null )
      {
        targetFilename = StudioCore.Compiling.m_LastBuildInfo.TargetFile;
      }
      else
      {
        targetFilename = Document.Element.TargetFilename;
        if ( !string.IsNullOrEmpty( Document.Element.CompileTargetFile ) )
        {
          targetFilename = Document.Element.CompileTargetFile;
        }
      }
      string targetPath = "";
      if ( !string.IsNullOrEmpty( targetFilename ) )
      {
        targetPath = System.IO.Path.GetDirectoryName( targetFilename );
      }
      if ( string.IsNullOrEmpty( targetPath ) )
      {
        targetPath = Document.Project.Settings.BasePath;
      }
      targetFilename = System.IO.Path.GetFileName( targetFilename );
      string targetFilenameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension( targetFilename );
      string fullTargetFilename = GR.Path.Append( targetPath, targetFilename );
      string fullTargetFilenameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension( fullTargetFilename );

      string runFilename = System.IO.Path.GetFileName( fullTargetFilename );
      string fullRunFilename = fullTargetFilename;
      string runPath = System.IO.Path.GetDirectoryName( fullRunFilename );

      // alternative run file name
      if ( ( FillForRunning )
      &&   ( Document.Element != null ) )
      {
        ProjectElement.PerConfigSettings configSettingRun = Document.Element.Settings[Document.Project.Settings.CurrentConfig.Name];
        if ( !string.IsNullOrEmpty( configSettingRun.DebugFile ) )
        {
          if ( configSettingRun.DebugFile.Contains( "$(Run" ) )
          {
            Error = true;
            StudioCore.AddToOutput( "Alternative run file name contains forbidden macro $(RunPath), $(RunFilename) or $(RunFilenameWithoutExtension)" + System.Environment.NewLine );
            return "";
          }
          fullRunFilename = FillParameters( configSettingRun.DebugFile, Document, false, out Error );
          if ( Error )
          {
            return "";
          }
          if ( !System.IO.Path.IsPathRooted( fullRunFilename ) )
          {
            // prepend build target path to filename
            fullRunFilename = System.IO.Path.Combine( targetPath, fullRunFilename );
          }
          runPath = System.IO.Path.GetDirectoryName( fullRunFilename );
          runFilename = System.IO.Path.GetFileName( fullRunFilename );
        }
      }
      string runFilenameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension( runFilename );
      string fullRunFilenameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension( fullRunFilename );



      result = result.Replace( "$(BuildTargetPath)", targetPath );
      result = result.Replace( "$(BuildTargetFilename)", fullTargetFilename );
      result = result.Replace( "$(BuildTargetFilenameWithoutExtension)", fullTargetFilenameWithoutExtension );
      result = result.Replace( "$(BuildTargetFile)", targetFilename );
      result = result.Replace( "$(BuildTargetFileWithoutExtension)", targetFilenameWithoutExtension );
      result = result.Replace( "$(RunPath)", runPath );
      result = result.Replace( "$(RunFilename)", fullRunFilename );
      result = result.Replace( "$(RunFilenameWithoutExtension)", fullRunFilenameWithoutExtension );
      if ( mainToolConfig.SelectedItem != null )
      {
        result = result.Replace( "$(ConfigName)", mainToolConfig.SelectedItem.ToString() );
      }
      if ( Document.Project != null )
      {
        result = result.Replace( "$(ProjectPath)", Document.Project.Settings.BasePath );
      }
      result = result.Replace( "$(MediaManager)", System.IO.Path.Combine( Application.StartupPath, "mediamanager.exe" ) );
      result = result.Replace( "$(MediaTool)", System.IO.Path.Combine( Application.StartupPath, "mediatool.exe" ) );

      int     debugStartAddress = StudioCore.Debugging.OverrideDebugStart;
      if ( debugStartAddress == -1 )
      {
        if ( Document.Project != null )
        {
          debugStartAddress = Document.Project.Settings.DebugStartAddress;
        }
      }
      result = result.Replace( "$(DebugStartAddress)", debugStartAddress.ToString() );
      result = result.Replace( "$(DebugStartAddressHex)", debugStartAddress.ToString( "x" ) );

      // replace symbols
      int dollarPos = result.IndexOf( "$(" );
      while ( dollarPos != -1 )
      {
        int macroEndPos = result.IndexOf( ')', dollarPos );

        if ( macroEndPos == -1 )
        {
          Error = true;
          StudioCore.AddToOutput( "Malformed Macro encountered at command " + Mask );
          return "";
        }
        string macroName = result.Substring( dollarPos + 2, macroEndPos - dollarPos - 2 );

        if ( Document.Type == ProjectElement.ElementType.ASM_SOURCE )
        {
          string    valueToInsert = "";
          if ( Document.ASMFileInfo.Labels.ContainsKey( macroName ) )
          {
            valueToInsert = Document.ASMFileInfo.Labels[macroName].AddressOrValue.ToString();
          }
          else
          {
            Error = true;
            StudioCore.AddToOutput( "Unknown macro " + macroName + " encountered at command " + Mask + System.Environment.NewLine );
            return "";
          }
          result = result.Substring( 0, dollarPos ) + valueToInsert + result.Substring( macroEndPos + 1 );
          macroEndPos = dollarPos + valueToInsert.Length;
        }
        else
        {
          Error = true;
          StudioCore.AddToOutput( "Unknown macro " + macroName + " encountered at command " + Mask + System.Environment.NewLine );
          return "";
        }
        dollarPos = result.IndexOf( "$(", macroEndPos + 1 );
      }
      return result;
    }



    bool RunCommand( DocumentInfo Doc, string StepDesc, string Command )
    {
      if ( !RunExternalCommand( Doc, Command ) )
      {
        StudioCore.AddToOutput( "-" + StepDesc + " step failed" + System.Environment.NewLine );
        return false;
      }
      StudioCore.AddToOutput( "-" + StepDesc + " step successful" + System.Environment.NewLine );
      return true;
    }



    private Types.CompileTargetType DetermineTargetType( DocumentInfo Doc, Parser.ParserBase Parser )
    {
      // compile target
      Types.CompileTargetType   compileTarget = C64Studio.Types.CompileTargetType.NONE;
      if ( Doc.Element != null )
      {
        compileTarget = Doc.Element.TargetType;
      }
      if ( compileTarget == C64Studio.Types.CompileTargetType.NONE )
      {
        compileTarget = Parser.CompileTarget;
      }
      return compileTarget;
    }



    private string DetermineTargetFilename( DocumentInfo Doc, Parser.ParserBase Parser )
    {
      if ( ( String.IsNullOrEmpty( Parser.CompileTargetFile ) )
      &&   ( ( Doc.Element == null )
      ||     ( String.IsNullOrEmpty( Doc.Element.TargetFilename ) ) ) )
      {
        // default to same name.prg and cbm
        if ( Doc.Project == null )
        {
          return System.IO.Path.Combine( System.IO.Path.GetDirectoryName( Doc.FullPath ), System.IO.Path.GetFileNameWithoutExtension( Doc.FullPath ) ) + ".prg";
        }
        return System.IO.Path.Combine( Doc.Project.Settings.BasePath, System.IO.Path.GetFileNameWithoutExtension( Doc.FullPath ) + ".prg" );
      }
      if ( ( Doc.Element != null )
      &&   ( !String.IsNullOrEmpty( Doc.Element.TargetFilename ) ) )
      {
        return GR.Path.Append( Doc.Project.Settings.BasePath, Doc.Element.TargetFilename );
      }
      return Parser.CompileTargetFile;
    }



    bool BuildElement( DocumentInfo Doc, string ConfigSetting, string AdditionalPredefines, out Types.BuildInfo BuildInfo, out Types.ASM.FileInfo FileInfo )
    {
      BuildInfo = new C64Studio.Types.BuildInfo();

      BuildInfo.TargetFile = "";
      BuildInfo.TargetType = Types.CompileTargetType.NONE;

      FileInfo = null;

      Types.ASM.FileInfo combinedFileInfo = null;

      if ( Doc.Element != null )
      {
        Doc.Element.CompileTarget = Types.CompileTargetType.NONE;
        Doc.Element.CompileTargetFile = null;

        // check dependencies
        foreach ( var dependency in Doc.Element.ForcedDependency.DependentOnFile )
        {
          ProjectElement elementDependency = Doc.Project.GetElementByFilename( dependency.Filename );
          if ( elementDependency == null )
          {
            StudioCore.AddToOutput( "Could not find dependency for " + dependency.Filename + System.Environment.NewLine );
            return false;
          }

          Types.ASM.FileInfo    dependencyFileInfo = null;

          // skip building if not required
          if ( !StudioCore.Compiling.NeedsRebuild( elementDependency.DocumentInfo, ConfigSetting ) )
          {
            StudioCore.AddToOutput( "Dependency " + dependency.Filename + " is current for config " + ConfigSetting + System.Environment.NewLine );

            if ( Doc.Type == ProjectElement.ElementType.ASM_SOURCE )
            {
              dependencyFileInfo = elementDependency.DocumentInfo.ASMFileInfo;
              //Debug.Log( "Doc " + Doc.Text + " receives " + dependencyFileInfo.Labels.Count + " dependency labels from dependency " + dependency.Filename );
            }
          }
          else
          {
            Types.BuildInfo tempInfo = new C64Studio.Types.BuildInfo();
            
            if ( !BuildElement( elementDependency.DocumentInfo, ConfigSetting, null, out tempInfo, out dependencyFileInfo ) )
            {
              return false;
            }
          }
          // include symbols from dependency
          if ( dependency.IncludeSymbols )
          {
            if ( combinedFileInfo == null )
            {
              combinedFileInfo = new C64Studio.Types.ASM.FileInfo();
            }
            // merge label info
            foreach ( var entry in dependencyFileInfo.Labels )
            {
              if ( !combinedFileInfo.Labels.ContainsKey( entry.Key ) )
              {
                combinedFileInfo.Labels.Add( entry.Key, entry.Value );
              }
                /*
              else
              {
                // override "old" info
                combinedFileInfo.Labels[entry.Key] = entry.Value;
              }*/
            }
            //Debug.Log( "Doc " + Doc.Text + " receives " + dependencyFileInfo.Labels.Count + " dependency labels from dependency " + dependency.Filename );
          }
        }
      }

      if ( !Doc.Compilable )
      {
        // not buildable 
        // TODO - Autoexport?
        return true;
      }

      ToolInfo tool = DetermineTool( Doc, false );

      ProjectElement.PerConfigSettings configSetting = null;

      Parser.ParserBase parser = DetermineParser( Doc );

      if ( Doc.Element != null )
      {
        if ( !Doc.Element.Settings.ContainsKey( ConfigSetting ) )
        {
          Doc.Element.Settings.Add( ConfigSetting, new ProjectElement.PerConfigSettings() );
        }
        configSetting = Doc.Element.Settings[ConfigSetting];

        if ( !string.IsNullOrEmpty( configSetting.PreBuild ) )
        {
          StudioCore.AddToOutput( "Running pre build step on " + Doc.Element.Name + System.Environment.NewLine );
          if ( !RunCommand( Doc, "pre build", configSetting.PreBuild ) )
          {
            return false;
          }
        }
        if ( configSetting.PreBuildChain.Active )
        {
          if ( !BuildChain( configSetting.PreBuildChain, "pre build chain" ) )
          {
            return false;
          }
        }
        StudioCore.AddToOutput( "Running build on " + Doc.Element.Name + " with configuration " + ConfigSetting + System.Environment.NewLine );
      }
      else
      {
        StudioCore.AddToOutput( "Running build on " + Doc.DocumentFilename + System.Environment.NewLine );
      }

      // include previous symbols
      if ( parser is Parser.ASMFileParser )
      {
        ( (Parser.ASMFileParser)parser ).InitialFileInfo = combinedFileInfo;
        if ( combinedFileInfo != null )
        {
          //Debug.Log( "Doc " + Doc.Text + " receives " + combinedFileInfo.Labels.Count + " initial labels" );
        }
        if ( !string.IsNullOrEmpty( AdditionalPredefines ) )
        {
          ( (Parser.ASMFileParser)parser ).ParseAndAddPreDefines( AdditionalPredefines );
        }
      }

      if ( ( configSetting != null )
      &&   ( !string.IsNullOrEmpty( configSetting.CustomBuild ) ) )
      {
        StudioCore.AddToOutput( "Running custom build step on " + Doc.Element.Name + " with configuration " + ConfigSetting + System.Environment.NewLine );
        if ( !RunCommand( Doc, "custom build", configSetting.CustomBuild ) )
        {
          return false;
        }
      }
      else
      {
        //EnsureFileIsParsed();
        //AddTask( new C64Studio.Tasks.TaskParseFile( baseDoc ) );

        ProjectConfig config = null;
        if ( Doc.Project != null )
        {
          config = Doc.Project.Settings.Configs[ConfigSetting];
        }

        if ( ( !ParseFile( parser, Doc, config ) )
        ||   ( !parser.Assemble( new C64Studio.Parser.CompileConfig()
                                        { 
                                          TargetType = DetermineTargetType( Doc, parser ), 
                                          OutputFile = DetermineTargetFilename( Doc, parser )
                                        }  ) )
        ||   ( parser.Errors > 0 ) )
        {
          AddOutputMessages( parser );

          StudioCore.AddToOutput( "Build failed, " + parser.Warnings.ToString() + " warnings, " + parser.Errors.ToString() + " errors encountered" + System.Environment.NewLine );
          StudioCore.Navigating.UpdateFromMessages( parser.Messages, 
                                                    ( parser is Parser.ASMFileParser ) ? ( (Parser.ASMFileParser)parser ).ASMFileInfo : null, 
                                                    Doc.Project );
          m_CompileResult.UpdateFromMessages( parser, Doc.Project );
          if ( !m_CompileResult.Visible )
          {
            m_CompileResult.Show();
          }
          AppState = Types.StudioState.NORMAL;

          if ( StudioCore.Settings.PlaySoundOnBuildFailure )
          {
            System.Media.SystemSounds.Exclamation.Play();
          }
          return false;
        }
        AddOutputMessages( parser );

        var compileTarget = DetermineTargetType( Doc, parser );
        string compileTargetFile = DetermineTargetFilename( Doc, parser );
        if ( Doc.Element != null )
        {
          Doc.Element.CompileTargetFile = compileTargetFile;
        }

        if ( compileTargetFile == null )
        {
          if ( parser is Parser.ASMFileParser )
          {
            parser.AddError( -1, Types.ErrorCode.E0001_NO_OUTPUT_FILENAME, "No output filename was given, missing element setting or !to <Filename>,<FileType> macro?" );
          }
          else
          {
            parser.AddError( -1, Types.ErrorCode.E0001_NO_OUTPUT_FILENAME, "No output filename was given, missing element setting" );
          }
          StudioCore.Navigating.UpdateFromMessages( parser.Messages,
                                          ( parser is Parser.ASMFileParser ) ? ( (Parser.ASMFileParser)parser ).ASMFileInfo : null,
                                          Doc.Project );

          m_CompileResult.UpdateFromMessages( parser, Doc.Project );
          if ( !m_CompileResult.Visible )
          {
            m_CompileResult.Show();
          }
          AppState = Types.StudioState.NORMAL;

          if ( StudioCore.Settings.PlaySoundOnBuildFailure )
          {
            System.Media.SystemSounds.Exclamation.Play();
          }
          return false;
        }
        BuildInfo.TargetFile = compileTargetFile;
        BuildInfo.TargetType = compileTarget;

        if ( parser.Warnings > 0 )
        {
          StudioCore.Navigating.UpdateFromMessages( parser.Messages,
                                          ( parser is Parser.ASMFileParser ) ? ( (Parser.ASMFileParser)parser ).ASMFileInfo : null,
                                          Doc.Project );

          m_CompileResult.UpdateFromMessages( parser, Doc.Project );

          if ( !m_CompileResult.Visible )
          {
            m_CompileResult.Show();
          }
        }
      }

      if ( string.IsNullOrEmpty( BuildInfo.TargetFile ) )
      {
        StudioCore.AddToOutput( "No target file name specified" + System.Environment.NewLine );
        AppState = Types.StudioState.NORMAL;
        if ( StudioCore.Settings.PlaySoundOnBuildFailure )
        {
          System.Media.SystemSounds.Exclamation.Play();
        }
        return false;
      }
      // write output if applicable
      if ( parser.Assembly != null )
      {
        try
        {
          System.IO.File.WriteAllBytes( BuildInfo.TargetFile, parser.Assembly.Data() );
        }
        catch ( System.Exception ex )
        {
          StudioCore.AddToOutput( "Build failed, Could not create output file " + parser.CompileTargetFile + System.Environment.NewLine );
          StudioCore.AddToOutput( ex.ToString() + System.Environment.NewLine );
          AppState = Types.StudioState.NORMAL;
          if ( StudioCore.Settings.PlaySoundOnBuildFailure )
          {
            System.Media.SystemSounds.Exclamation.Play();
          }
          return false;
        }
        StudioCore.AddToOutput( "Build successful, " + parser.Warnings.ToString() + " warnings, 0 errors encountered, compiled to file " + BuildInfo.TargetFile + ", " + parser.Assembly.Length + " bytes" + System.Environment.NewLine );

        //Debug.Log( "File " + Doc.DocumentFilename + " was rebuilt for config " + ConfigSetting + " this round" );
      }

      if ( ( configSetting != null )
      &&   ( configSetting.PostBuildChain.Active ) )
      {
        if ( !BuildChain( configSetting.PostBuildChain, "post build chain" ) )
        {
          return false;
        }
      }


      if ( ( configSetting != null )
      &&   ( !string.IsNullOrEmpty( configSetting.PostBuild ) ) )
      {
        m_Output.Show();

        StudioCore.AddToOutput( "Running post build step on " + Doc.Element.Name + System.Environment.NewLine );
        if ( !RunCommand( Doc, "post build", configSetting.PostBuild ) )
        {
          return false;
        }
      }

      Doc.HasBeenSuccessfullyBuilt = true;

      if ( parser is Parser.ASMFileParser )
      {
        FileInfo = ( (Parser.ASMFileParser)parser ).ASMFileInfo;
        // update symbols in main asm file
        Doc.SetASMFileInfo( FileInfo, parser.KnownTokens(), parser.KnownTokenInfo() );
        //Debug.Log( "Doc " + Doc.Text + " gets " + ( (SourceASM)Doc ).ASMFileInfo.Labels.Count + " labels" );
      }

      if ( FileInfo != null )
      {
        if ( !string.IsNullOrEmpty( FileInfo.LabelDumpFile ) )
        {
          DumpLabelFile( FileInfo );
        }
      }

      StudioCore.Compiling.m_RebuiltFiles.Add( Doc.DocumentFilename );
      return true;
    }



    private bool BuildChain( Types.BuildChain BuildChain, string BuildChainDescription )
    {
      if ( StudioCore.Compiling.m_BuildChainStack.Contains( BuildChain ) )
      {
        // already on stack, silent "success"
        return true;
      }

      StudioCore.AddToOutput( "Running " + BuildChainDescription + System.Environment.NewLine );
      StudioCore.Compiling.m_BuildChainStack.Push( BuildChain );
      foreach ( var entry in BuildChain.Entries )
      {
        BuildInfo                     buildInfo;
        Types.ASM.FileInfo            fileInfo;

        string  buildInfoKey = entry.ProjectName + "/" + entry.DocumentFilename + "/" + entry.Config;

        StudioCore.AddToOutput( "Building " + buildInfoKey + System.Environment.NewLine );
        if ( StudioCore.Compiling.m_RebuiltBuildConfigFiles.ContainsValue( buildInfoKey ) )
        {
          StudioCore.AddToOutput( "-already built, skipping step" + System.Environment.NewLine );
          continue;
        }

        var project = m_Solution.GetProjectByName( entry.ProjectName );
        if ( project == null )
        {
          StudioCore.AddToOutput( "-could not find referenced project " + entry.ProjectName + System.Environment.NewLine );
          StudioCore.Compiling.m_BuildChainStack.Pop();
          return false;
        }

        var element = project.GetElementByFilename( entry.DocumentFilename );
        if ( element == null )
        {
          StudioCore.AddToOutput( "-could not find document " + entry.DocumentFilename + " in project " + entry.ProjectName + System.Environment.NewLine );
          StudioCore.Compiling.m_BuildChainStack.Pop();
          return false;
        }

        // ugly hack to force rebuild -> problem: we do not check output file timestamps if we need to recompile -> can't have build chain with same file in different configs!
        MarkAsDirty( element.DocumentInfo );

        if ( !BuildElement( element.DocumentInfo, entry.Config, entry.PreDefines, out buildInfo, out fileInfo ) )
        {
          StudioCore.Compiling.m_BuildChainStack.Pop();
          return false;
        }
        StudioCore.Compiling.m_RebuiltBuildConfigFiles.Add( buildInfoKey );
      }
      StudioCore.AddToOutput( "Running " + BuildChainDescription + " completed successfully" + System.Environment.NewLine );
      StudioCore.Compiling.m_BuildChainStack.Pop();
      return true;
    }



    private void DumpLabelFile( Types.ASM.FileInfo FileInfo )
    {
      StringBuilder   sb = new StringBuilder();

      foreach ( var labelInfo in FileInfo.Labels )
      {
        sb.Append( labelInfo.Value.Name );
        sb.Append( " =$" );
        if ( labelInfo.Value.AddressOrValue > 255 )
        {
          sb.Append( labelInfo.Value.AddressOrValue.ToString( "X4" ) );
        }
        else
        {
          sb.Append( labelInfo.Value.AddressOrValue.ToString( "X2" ) );
        }
        sb.Append( "; " );
        if ( !labelInfo.Value.Used )
        {
          sb.AppendLine( "unused" );
        }
        else
        {
          sb.AppendLine();
        }
      }
      GR.IO.File.WriteAllText( FileInfo.LabelDumpFile, sb.ToString() );
    }



    private void MarkAsDirty( DocumentInfo DocInfo )
    {
      if ( DocInfo == null )
      {
        return;
      }
      if ( !DocInfo.HasBeenSuccessfullyBuilt )
      {
        return;
      }
      DocInfo.HasBeenSuccessfullyBuilt = false;

      if ( DocInfo.Element != null )
      {
        foreach ( var dependency in DocInfo.Element.ForcedDependency.DependentOnFile )
        {
          ProjectElement elementDependency = DocInfo.Project.GetElementByFilename( dependency.Filename );
          if ( elementDependency == null )
          {
            return;
          }
          MarkAsDirty( elementDependency.DocumentInfo );
        }
      }
      if ( !DocInfo.DeducedDependency.ContainsKey( DocInfo.Project.Settings.CurrentConfig.Name ) )
      {
        DocInfo.DeducedDependency.Add( DocInfo.Project.Settings.CurrentConfig.Name, new DependencyBuildState() );
      }
      foreach ( var deducedDependency in DocInfo.DeducedDependency[DocInfo.Project.Settings.CurrentConfig.Name].BuildState )
      {
        ProjectElement elementDependency = DocInfo.Project.GetElementByFilename( deducedDependency.Key );
        if ( elementDependency == null )
        {
          return;
        }
        MarkAsDirty( elementDependency.DocumentInfo );
      }
    }



    private bool StartCompile( DocumentInfo DocumentToBuild, DocumentInfo DocumentToDebug, DocumentInfo DocumentToRun )
    {
      StudioCore.SetStatus( "Building..." );
      StudioCore.Compiling.m_RebuiltFiles.Clear();
      StudioCore.Compiling.m_RebuiltBuildConfigFiles.Clear();
      StudioCore.Compiling.m_BuildChainStack.Clear();
      bool needsRebuild = StudioCore.Compiling.NeedsRebuild( DocumentToBuild );
      if ( needsRebuild )
      {
        StudioCore.Compiling.m_BuildIsCurrent = false;
        if ( DocumentToBuild.Project != null )
        {
          if ( !SaveProject( DocumentToBuild.Project ) )
          {
            StudioCore.SetStatus( "Failed to save project" );
            return false;
          }
        }
        SaveAllDocuments();
        if ( DocumentToBuild.Project != null )
        {
          if ( !SaveProject( DocumentToBuild.Project ) )
          {
            StudioCore.SetStatus( "Failed to save project" );
            return false;
          }
        }
      }

      DocumentInfo baseDoc = DocumentToBuild;
      if ( baseDoc == null )
      {
        StudioCore.SetStatus( "No active document" );
        return false;
      }
      if ( ( baseDoc.Element == null )
      &&   ( !baseDoc.Compilable ) )
      {
        StudioCore.AddToOutput( "Document is not part of project, cannot build" + System.Environment.NewLine );
        StudioCore.SetStatus( "Document is not part of project, cannot build" );
        return false;
      }
      m_Output.SetText( "" );
      StudioCore.AddToOutput( "Determined " + baseDoc.DocumentFilename + " as active document" + System.Environment.NewLine );

      Types.BuildInfo buildInfo = new C64Studio.Types.BuildInfo();
      if ( !StudioCore.Compiling.m_BuildIsCurrent )
      {
        C64Studio.Types.ASM.FileInfo    dummyInfo;

        string  configSetting = null;
        if ( baseDoc.Project != null )
        {
          configSetting = baseDoc.Project.Settings.CurrentConfig.Name;
        }

        if ( !BuildElement( baseDoc, configSetting, null, out buildInfo, out dummyInfo ) )
        {
          StudioCore.SetStatus( "Build failed" );
          return false;
        }
        StudioCore.Compiling.m_LastBuildInfo = buildInfo;
        StudioCore.Compiling.m_BuildIsCurrent = true;
      }
      else
      {
        if ( baseDoc.Element != null )
        {
          buildInfo.TargetType = baseDoc.Element.TargetType;
        }
        else
        {
          buildInfo = StudioCore.Compiling.m_LastBuildInfo;
        }
        if ( buildInfo.TargetType == C64Studio.Types.CompileTargetType.NONE )
        {
          buildInfo.TargetType = StudioCore.Compiling.m_LastBuildInfo.TargetType;
        }

        StudioCore.AddToOutput( "Build is current" + System.Environment.NewLine );
      }
      if ( StudioCore.Navigating.DetermineASMFileInfo( baseDoc ) == StudioCore.Navigating.DetermineASMFileInfo( ActiveDocumentInfo ) )
      {
        //Debug.Log( "m_Outline.RefreshFromDocument after compile" );
        m_Outline.RefreshFromDocument( baseDoc.BaseDoc );
      }
      StudioCore.SetStatus( "Build successful" );

      switch ( AppState )
      {
        case Types.StudioState.COMPILE:
        case Types.StudioState.BUILD:
          AppState = Types.StudioState.NORMAL;
          if ( StudioCore.Settings.PlaySoundOnSuccessfulBuild )
          {
            System.Media.SystemSounds.Asterisk.Play();
          }
          break;
        case Types.StudioState.BUILD_AND_RUN:
          // run program
          {
            Types.CompileTargetType targetType = buildInfo.TargetType;
            if ( DocumentToRun.Element != null )
            {
              if ( DocumentToRun.Element.TargetType != C64Studio.Types.CompileTargetType.NONE )
              {
                targetType = DocumentToRun.Element.TargetType;
              }
              ProjectElement.PerConfigSettings  configSetting = DocumentToRun.Element.Settings[DocumentToRun.Project.Settings.CurrentConfig.Name];
              if ( !string.IsNullOrEmpty( configSetting.DebugFile ) )
              {
                targetType = configSetting.DebugFileType;
              }
            }
            if ( !RunCompiledFile( DocumentToRun, targetType ) )
            {
              AppState = Types.StudioState.NORMAL;
              return false;
            }
          }
          break;
        case Types.StudioState.BUILD_AND_DEBUG:
          // run program
          if ( !DebugCompiledFile( DocumentToDebug, DocumentToRun ) )
          {
            AppState = Types.StudioState.NORMAL;
            return false;
          }
          break;
        default:
          AppState = Types.StudioState.NORMAL;
          break;
      }

      /*
      CompilerProcess = new System.Diagnostics.Process();
      CompilerProcess.StartInfo.FileName = tool.Filename;
      CompilerProcess.StartInfo.WorkingDirectory = FillParameters( tool.WorkPath, baseDoc );
      CompilerProcess.StartInfo.CreateNoWindow = true;
      CompilerProcess.EnableRaisingEvents = true;
      CompilerProcess.StartInfo.Arguments = "";
      if ( AppState == State.COMPILE_AND_DEBUG )
      {
        CompilerProcess.StartInfo.Arguments += FillParameters( tool.DebugArguments, baseDoc );
      }
      CompilerProcess.StartInfo.Arguments += " " + FillParameters( tool.Arguments, baseDoc );
      CompilerProcess.StartInfo.UseShellExecute = false;
      CompilerProcess.StartInfo.RedirectStandardError = true;
      CompilerProcess.StartInfo.RedirectStandardOutput = true;
      CompilerProcess.StartInfo.RedirectStandardInput = true;
      CompilerProcess.Exited += new EventHandler( compilerProcess_Exited );

      CompilerProcess.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler( compilerProcess_OutputDataReceived );
      CompilerProcess.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler( compilerProcess_OutputDataReceived );

      try
      {
        SetGUIForWaitOnExternalTool( true );
        CompilerProcess.Start();

        CompilerProcess.BeginOutputReadLine();
        CompilerProcess.BeginErrorReadLine();

        System.IO.StreamWriter writer = CompilerProcess.StandardInput;

        writer.Write( baseDoc.GetContent() );
        writer.Close();
      }
      catch ( Win32Exception ex )
      {
        CompilerProcess.Close();
        AddToOutput( ex.Message );
        SetGUIForWaitOnExternalTool( false );
        return false;
      }
       */
      return true;
    }



    private bool RunExternalCommand( string Command, DocumentInfo CommandDocument )
    {
      m_LastReceivedOutputTime = System.DateTime.Now;

      string    fullCommand = Command;
      string    args = "";
      if ( Command.StartsWith( "\"" ) )
      {
        int   nextQuote = Command.IndexOf( '"', 1 );
        if ( nextQuote == -1 )
        {
          // invalid file
          StudioCore.AddToOutput( "Invalid command specified (" + Command + ")" );
          return false;
        }
        fullCommand = Command.Substring( 1, nextQuote - 1 );
        args = Command.Substring( nextQuote + 1 ).Trim();
      }
      else if ( Command.IndexOf( ' ' ) != -1 )
      {
        int   spacePos = Command.IndexOf( ' ' );
        fullCommand = Command.Substring( 0, spacePos );
        args = Command.Substring( spacePos + 1 ).Trim();
      }

      fullCommand = "cmd.exe";

      bool error = false;
      bool errorAtArgs = false;

      string    command = FillParameters( Command, CommandDocument, false, out error );
      //fullCommand = FillParameters( fullCommand, CommandDocument, false, out error );
      args = "/C \"" + command + "\"";
      args = FillParameters( args, CommandDocument, false, out errorAtArgs );
      if ( ( error )
      ||   ( errorAtArgs ) )
      {
        return false;
      }
      

      //Debug.Log( "Args:" + args );
      //string command = fullCommand + " " + args;

      StudioCore.AddToOutput( command + System.Environment.NewLine );

      m_ExternalProcess = new System.Diagnostics.Process();
      m_ExternalProcess.StartInfo.FileName = fullCommand;
      m_ExternalProcess.StartInfo.WorkingDirectory = FillParameters( "$(BuildTargetPath)", CommandDocument, false, out error );

      if ( error )
      {
        return false;
      }
      if ( !System.IO.Directory.Exists( m_ExternalProcess.StartInfo.WorkingDirectory + "/" ) )
      {
        StudioCore.AddToOutput( "The determined working directory \"" + m_ExternalProcess.StartInfo.WorkingDirectory + "\" does not exist" + System.Environment.NewLine );
        return false;
      }

      m_ExternalProcess.StartInfo.CreateNoWindow = true;
      m_ExternalProcess.EnableRaisingEvents = true;
      m_ExternalProcess.StartInfo.Arguments = args;
      m_ExternalProcess.StartInfo.UseShellExecute = false;
      m_ExternalProcess.Exited += new EventHandler( m_ExternalProcess_Exited );

      m_ExternalProcess.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler( compilerProcess_OutputDataReceived);
      m_ExternalProcess.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler( compilerProcess_OutputDataReceived );
      m_ExternalProcess.StartInfo.RedirectStandardError = true;
      m_ExternalProcess.StartInfo.RedirectStandardOutput = true;

      try
      {
        if ( !m_ExternalProcess.Start() )
        {
          m_ExternalProcess.Close();
          return false;
        }
        m_ExternalProcess.BeginOutputReadLine();
        m_ExternalProcess.BeginErrorReadLine();
      }
      catch ( Win32Exception ex )
      {
        m_ExternalProcess.Close();
        StudioCore.AddToOutput( ex.Message + System.Environment.NewLine );
        return false;
      }

      //Debug.Log( "=============Start" );
      while ( !m_ExternalProcess.WaitForExit( 50 ) )
      {
        Application.DoEvents();
      }
      // DO NOT REMOVE: final DoEvents to let the app clear its invoke queue to the output display 
      Application.DoEvents();
      //Debug.Log( "=============Done" );

      /*
      // working wait
      while ( ( System.DateTime.Now - m_LastReceivedOutputTime ).TotalMilliseconds < 500  )
      {
        Application.DoEvents();
        System.Threading.Thread.Sleep( 20 );
      }
       */

      bool success = ( m_ExternalProcess.ExitCode == 0 );
      if ( !success )
      {
        StudioCore.AddToOutput( "External Command " + command + " exited with result code " + m_ExternalProcess.ExitCode.ToString() + System.Environment.NewLine );
      }
      m_ExternalProcess.Close();
      return success;
    }



    private bool RunExternalCommand( DocumentInfo Doc, string Command )
    {
      string[]    commands = System.Text.RegularExpressions.Regex.Split( Command, System.Environment.NewLine );
      //Debug.Log( "Runexternalcommand " + Command );

      SetGUIForWaitOnExternalTool( true );
      foreach ( string command in commands )
      {
        if ( string.IsNullOrEmpty( command.Trim() ) )
        {
          continue;
        }
        if ( !RunExternalCommand( command, Doc ) )
        {
          SetGUIForWaitOnExternalTool( false );
          return false;
        }
      }
      SetGUIForWaitOnExternalTool( false );
      return true;
    }



    void m_ExternalProcess_Exited( object sender, EventArgs e )
    {
      /*
      System.Diagnostics.Process    process = (System.Diagnostics.Process)sender;
      string    output = process.StandardOutput.ReadToEnd();
      string    error = process.StandardError.ReadToEnd();

      AddToOutput( output );
      AddToOutput( error );*/
      /*
      System.Diagnostics.Process    process = (System.Diagnostics.Process)sender;
      int     exitCode = process.ExitCode;
      AddToOutput( "Tool Exited with result code " + exitCode.ToString() + System.Environment.NewLine );
      process.Close();
      SetGUIForWaitOnExternalTool( false );

      if ( exitCode != 0 )
      {
        // update errors/warnings
        AppState = State.NORMAL;
        return;
      }
      AppState = State.NORMAL;
       * */
    }



    public void Rebuild( DocumentInfo DocInfo )
    {
      if ( AppState != Types.StudioState.NORMAL )
      {
        return;
      }

      MarkAsDirty( DocInfo );

      AppState = Types.StudioState.BUILD;
      StudioCore.Debugging.OverrideDebugStart = -1;
      if ( !StartCompile( DocInfo, null, null ) )
      {
        AppState = Types.StudioState.NORMAL;
      }
    }



    public void Build( DocumentInfo Document )
    {
      if ( AppState != Types.StudioState.NORMAL )
      {
        return;
      }
      AppState = Types.StudioState.BUILD;
      StudioCore.Debugging.OverrideDebugStart = -1;
      if ( !StartCompile( Document, null, null ) )
      {
        AppState = Types.StudioState.NORMAL;
      }
    }



    private void Compile( DocumentInfo Document )
    {
      if ( AppState != Types.StudioState.NORMAL )
      {
        return;
      }
      AppState = Types.StudioState.COMPILE;
      StudioCore.Debugging.OverrideDebugStart = -1;
      if ( !StartCompile( Document, null, null ) )
      {
        AppState = Types.StudioState.NORMAL;
      }
    }



    private void mainToolCompile_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.BUILD );
    }



    public void AddToOutputAndShow( string Text )
    {
      if ( InvokeRequired )
      {
        try
        {
          Invoke( new AddToOutputAndShowCallback( AddToOutputAndShow ), new object[] { Text } );
        }
        catch ( System.ObjectDisposedException )
        {
        }
      }
      else
      {
        m_Output.AppendText( Text );
        m_Output.Show();
      }
    }



    public void AddOutputMessages( Parser.ParserBase Parser )
    {
      foreach ( System.Collections.Generic.KeyValuePair<int, Parser.ParserBase.ParseMessage> msg in Parser.Messages )
      {
        Parser.ParserBase.ParseMessage message = msg.Value;
        if ( message.Type == C64Studio.Parser.ParserBase.ParseMessage.LineType.MESSAGE )
        {
          StudioCore.AddToOutput( message.Message + System.Environment.NewLine );
        }
      }
    }



    void compilerProcess_OutputDataReceived( object sender, System.Diagnostics.DataReceivedEventArgs e )
    {
      m_LastReceivedOutputTime = System.DateTime.Now;
      if ( !String.IsNullOrEmpty( e.Data ) )
      {
        StudioCore.AddToOutput( e.Data + System.Environment.NewLine );
      }
    }



    void ReadLabelsFromFile( string Filename )
    {
      StudioCore.Debugging.Debugger.ClearLabels();
      try
      {
        string[] labelInfos = System.IO.File.ReadAllLines( Filename );

        foreach ( string labelInfo in labelInfos )
        {
          int equPos = labelInfo.IndexOf( '=' );
          int semPos = labelInfo.IndexOf( ';' );
          if ( equPos != -1 )
          {
            string  labelName = labelInfo.Substring( 0, equPos ).Trim();
            string  labelValueText = "";
            int     labelValue = -1;
            if ( semPos == -1 )
            {
              labelValueText = labelInfo.Substring( equPos + 1 ).Trim();
            }
            else
            {
              labelValueText = labelInfo.Substring( equPos + 1, semPos - equPos - 1 ).Trim();
            }
            if ( labelValueText.StartsWith( "$" ) )
            {
              labelValue = System.Convert.ToInt32( labelValueText.Substring( 1 ), 16 );
            }
            else
            {
              int.TryParse( labelValueText, out labelValue );
            }
            StudioCore.Debugging.Debugger.AddLabel( labelName, labelValue );
            //dh.Log( "Label: " + labelName + "=" + labelValue );
          }
        }
      }
      catch ( System.IO.IOException io )
      {
        StudioCore.AddToOutput( "ReadLabelsFromFile failed: " + io.ToString() + System.Environment.NewLine );
      }
    }



    private bool CheckViceVersion( ToolInfo toolRun )
    {
      System.Diagnostics.FileVersionInfo    fileVersion;
      try
      {
        fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo( toolRun.Filename );
      }
      catch ( System.Exception io )
      {
        StudioCore.AddToOutput( "Could not check emulator version: " + io.Message );
        return false;
      }
      StudioCore.Debugging.Debugger.m_BinaryMemDump = false;
      if ( ( fileVersion.ProductVersion == "2.3" )
      ||   ( fileVersion.ProductVersion.StartsWith( "2.3." ) ) )
      {
        StudioCore.Debugging.Debugger.m_ViceVersion = RemoteDebugger.WinViceVersion.V_2_3;
      }
      else if ( ( fileVersion.ProductVersion == "2.4" )
      ||        ( fileVersion.ProductVersion.StartsWith( "2.4." ) ) )
      {
        StudioCore.Debugging.Debugger.m_ViceVersion = RemoteDebugger.WinViceVersion.V_2_4;
      }
      else if ( ( fileVersion.ProductVersion == "3.0" )
      ||        ( fileVersion.ProductVersion.StartsWith( "3.0." ) ) )
      {
        StudioCore.Debugging.Debugger.m_ViceVersion = RemoteDebugger.WinViceVersion.V_3_0;
        StudioCore.Debugging.Debugger.m_BinaryMemDump = true;
      }
      return true;
    }



    private bool RunCompiledFile( DocumentInfo Document, Types.CompileTargetType TargetType )
    {
      if ( Document.Element != null )
      {
        StudioCore.AddToOutput( "Running " + Document.Element.Name + System.Environment.NewLine );
      }
      else
      {
        StudioCore.AddToOutput( "Running " + Document.DocumentFilename + System.Environment.NewLine );
      }

      ToolInfo toolRun = DetermineTool( Document, true );
      if ( toolRun == null )
      {
        System.Windows.Forms.MessageBox.Show( "No emulator tool has been configured yet!", "Missing emulator tool" );
        StudioCore.AddToOutput( "There is no emulator tool configured!" );
        return false;
      }

      // check file version (WinVICE remote debugger changes)
      if ( !CheckViceVersion( toolRun ) )
      {
        return false;
      }

      bool error = false;

      RunProcess = new System.Diagnostics.Process();
      RunProcess.StartInfo.FileName = toolRun.Filename;
      RunProcess.StartInfo.WorkingDirectory = FillParameters( toolRun.WorkPath, Document, true, out error );
      RunProcess.EnableRaisingEvents = true;

      if ( error )
      {
        return false;
      }

      if ( !System.IO.Directory.Exists( RunProcess.StartInfo.WorkingDirectory.Trim( new char[]{ '\"' } ) ) )
      {
        StudioCore.AddToOutput( "The determined working directory" + RunProcess.StartInfo.WorkingDirectory + " does not exist" + System.Environment.NewLine );
        return false;
      }

      string    runArguments = toolRun.PRGArguments;
      if ( ( TargetType == Types.CompileTargetType.CARTRIDGE_MAGICDESK_BIN )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_MAGICDESK_CRT )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_EASYFLASH_BIN )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_EASYFLASH_CRT )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_RGCD_BIN )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_RGCD_CRT )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_16K_BIN )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_16K_CRT )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_8K_BIN )
      ||   ( TargetType == Types.CompileTargetType.CARTRIDGE_8K_CRT ) )
      {
        runArguments = toolRun.CartArguments;
      }
      if ( StudioCore.Settings.TrueDriveEnabled )
      {
        runArguments = toolRun.TrueDriveOnArguments + " " + runArguments;
      }
      else
      {
        runArguments = toolRun.TrueDriveOffArguments + " " + runArguments;
      }

      if ( ( Document != null )
      &&   ( Document.ASMFileInfo != null )
      &&   ( toolRun.PassLabelsToEmulator ) )
      {
        string  labelInfo = Document.ASMFileInfo.LabelsAsFile();
        if ( labelInfo.Length > 0 )
        {
          try
          {
            StudioCore.Debugging.TempDebuggerStartupFilename = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText( StudioCore.Debugging.TempDebuggerStartupFilename, labelInfo );
            runArguments = "-moncommands \"" + StudioCore.Debugging.TempDebuggerStartupFilename + "\" " + runArguments;
          }
          catch ( System.IO.IOException ioe )
          {
            System.Windows.Forms.MessageBox.Show( ioe.Message, "Error writing temporary file" );
            StudioCore.AddToOutput( "Error writing temporary file" );
            StudioCore.Debugging.TempDebuggerStartupFilename = "";
            return false;
          }
        }
      }
      RunProcess.StartInfo.Arguments = FillParameters( runArguments, Document, true, out error );
      if ( error )
      {
        return false;
      }

      RunProcess.Exited += new EventHandler( runProcess_Exited );
      StudioCore.AddToOutput( "Calling " + RunProcess.StartInfo.FileName + " with " + RunProcess.StartInfo.Arguments + System.Environment.NewLine );

      SetGUIForWaitOnExternalTool( true );
      return RunProcess.Start();
    }



    private Types.Breakpoint BreakpointAtAddress( int Address )
    {
      foreach ( var dock in StudioCore.Debugging.BreakPoints.Keys )
      {
        foreach ( var bp in StudioCore.Debugging.BreakPoints[dock] )
        {
          if ( bp.Address == Address )
          {
            return bp;
          }
        }
      }
      return null;
    }



    private void AddVirtualBreakpoints( Types.ASM.FileInfo ASMFileInfo )
    {
      foreach ( var virtualBP in ASMFileInfo.VirtualBreakpoints.Values )
      {
        virtualBP.IsVirtual = true;
        int globalLineIndex = -1;
        if ( !ASMFileInfo.FindGlobalLineIndex( virtualBP.LineIndex, virtualBP.DocumentFilename, out globalLineIndex ) )
        {
          StudioCore.AddToOutput( "Cannot assign breakpoint for line " + virtualBP.LineIndex + ", no address found" + System.Environment.NewLine );
          continue;
        }
        int address = ASMFileInfo.FindLineAddress( globalLineIndex );
        if ( address != -1 )
        {
          var existingBP = BreakpointAtAddress( address );

          if ( existingBP == null )
          {
            C64Studio.Types.Breakpoint bp = new C64Studio.Types.Breakpoint();

            bp.LineIndex = virtualBP.LineIndex;
            bp.Address = address;
            bp.TriggerOnExec = true;
            bp.IsVirtual = true;
            bp.DocumentFilename = virtualBP.DocumentFilename;
            bp.Virtual.Add( virtualBP );
            virtualBP.Address = address;
            // we just need any key (as null is not allowed)
            if ( !StudioCore.Debugging.BreakPoints.ContainsKey( "C64Studio.DebugBreakpoints" ) )
            {
              StudioCore.Debugging.BreakPoints.Add( "C64Studio.DebugBreakpoints", new List<C64Studio.Types.Breakpoint>() );
            }
            StudioCore.Debugging.BreakPoints["C64Studio.DebugBreakpoints"].Add( bp );
            //AddBreakpoint( bp );
            Debug.Log( "Add virtual bp for $" + address.ToString( "X4" ) );
          }
          else
          {
            // merge with existing
            existingBP.TriggerOnExec = true;
            existingBP.Virtual.Add( virtualBP );
          }
        }
        else
        {
          StudioCore.AddToOutput( "Cannot assign breakpoint for line " + virtualBP.LineIndex + ", no address found" + System.Environment.NewLine );
        }
      }
    }



    private void RemoveVirtualBreakpoints()
    {
      foreach ( var key in StudioCore.Debugging.BreakPoints.Keys )
      {
        repeat:
        foreach ( Types.Breakpoint breakPoint in StudioCore.Debugging.BreakPoints[key] )
        {
          if ( !breakPoint.HasNonVirtual() )
          {
            StudioCore.Debugging.BreakPoints[key].Remove( breakPoint );
            goto repeat;
          }
        }
      }
    }



    private void ReseatBreakpoints( Types.ASM.FileInfo ASMFileInfo )
    {
      foreach ( var key in StudioCore.Debugging.BreakPoints.Keys )
      {
        foreach ( Types.Breakpoint breakPoint in StudioCore.Debugging.BreakPoints[key] )
        {
          breakPoint.RemoteIndex = -1;
          breakPoint.IsVirtual = false;
          breakPoint.Virtual.Clear();
          breakPoint.Virtual.Add( breakPoint );

          if ( key != "C64Studio.DebugBreakpoints" )
          {
            breakPoint.Address = -1;
            int globalLineIndex = 0;
            if ( ASMFileInfo.FindGlobalLineIndex( breakPoint.LineIndex, breakPoint.DocumentFilename, out globalLineIndex ) )
            {
              int address = ASMFileInfo.FindLineAddress( globalLineIndex );
              if ( breakPoint.Address != address )
              {
                breakPoint.Address = address;

                Document_DocumentEvent( new BaseDocument.DocEvent( BaseDocument.DocEvent.Type.BREAKPOINT_UPDATED, breakPoint ) );
              }
              if ( address != -1 )
              {
                //Debug.Log( "Found breakpoint at address " + address );
              }
            }
            else if ( breakPoint.AddressSource != null )
            {
              var address = ASMFileInfo.AddressFromToken( breakPoint.AddressSource );
              if ( address != -1 )
              {
                breakPoint.Address = address;

                Document_DocumentEvent( new BaseDocument.DocEvent( BaseDocument.DocEvent.Type.BREAKPOINT_UPDATED, breakPoint ) );
              }
            }
          }
        }
      }
    }



    private bool EmulatorSupportsDebugging( ToolInfo Emulator )
    {
      return System.IO.Path.GetFileNameWithoutExtension( Emulator.Filename ).ToUpper().StartsWith( "X64" );    
    }



    private bool DebugCompiledFile( DocumentInfo DocumentToDebug, DocumentInfo DocumentToRun )
    {
      if ( DocumentToDebug.Element == null )
      {
        StudioCore.AddToOutput( "Debugging " + DocumentToDebug.DocumentFilename + System.Environment.NewLine );
      }
      else
      {
        StudioCore.AddToOutput( "Debugging " + DocumentToDebug.Element.Name + System.Environment.NewLine );
      }

      ToolInfo toolRun = DetermineTool( DocumentToRun, true );
      if ( toolRun == null )
      {
        System.Windows.Forms.MessageBox.Show( "No emulator tool has been configured yet!", "Missing emulator tool" );
        StudioCore.AddToOutput( "There is no emulator tool configured!" );
        return false;
      }

      if ( !CheckViceVersion( toolRun ) )
      {
        return false;
      }

      StudioCore.Debugging.DebuggedASMBase      = DocumentToDebug;
      StudioCore.Debugging.DebugBaseDocumentRun = DocumentToRun;

      m_DebugWatch.ReseatWatches( DocumentToDebug.ASMFileInfo );
      StudioCore.Debugging.Debugger.ClearCaches();
      ReseatBreakpoints( DocumentToDebug.ASMFileInfo );
      AddVirtualBreakpoints( DocumentToDebug.ASMFileInfo );
      StudioCore.Debugging.Debugger.SetBreakPoints( StudioCore.Debugging.BreakPoints );

      /*
      Debug.Log( "Breakpoints." );
      foreach ( var bplist in m_BreakPoints.Values )
      {
        foreach ( var bp in bplist )
        {
          Debug.Log( "BP at " + bp.LineIndex + " in " + bp.DocumentFilename + " at " + bp.Address + "(" + bp.Address.ToString( "x4" ) + ") V=" + bp.IsVirtual );
          foreach ( var bpchild in bp.Virtual )
          {
            Debug.Log( "-BP at " + bpchild.LineIndex + " in " + bpchild.DocumentFilename + " at " + bpchild.Address + "(" + bp.Address.ToString( "x4" ) + ") V=" + bpchild.IsVirtual );
          }
        }
      }*/

      StudioCore.Debugging.MarkedDocument     = null;
      StudioCore.Debugging.MarkedDocumentLine = -1;

      bool error = false;

      RunProcess = new System.Diagnostics.Process();
      RunProcess.StartInfo.FileName = toolRun.Filename;
      RunProcess.StartInfo.WorkingDirectory = FillParameters( toolRun.WorkPath, DocumentToRun, true, out error );
      RunProcess.EnableRaisingEvents = true;

      if ( error )
      {
        return false;
      }
      if ( !System.IO.Directory.Exists( RunProcess.StartInfo.WorkingDirectory.Trim( new char[]{ '"' } ) ) )
      {
        StudioCore.AddToOutput( "The determined working directory " + RunProcess.StartInfo.WorkingDirectory + " does not exist" + System.Environment.NewLine );
        return false;
      }

      StudioCore.Debugging.BreakpointsToAddAfterStartup.Clear();

      string  breakPointFile = "";
      int     remoteIndex = 2;    // 1 is the init breakpoint
      foreach ( var key in StudioCore.Debugging.BreakPoints.Keys )
      {
        foreach ( Types.Breakpoint breakPoint in StudioCore.Debugging.BreakPoints[key] )
        {
          if ( key != "C64Studio.DebugBreakpoints" )
          {
            bool mustBeAddedLater = false;

            if ( breakPoint.Address != -1 )
            {
              if ( breakPoint.TriggerOnLoad )
              {
                // store for later addition
                StudioCore.Debugging.BreakpointsToAddAfterStartup.Add( breakPoint );
                mustBeAddedLater = true;
              }
              if ( breakPoint.TriggerOnStore )
              {
                if ( !StudioCore.Debugging.BreakpointsToAddAfterStartup.Contains( breakPoint ) )
                {
                  // store for later addition
                  StudioCore.Debugging.BreakpointsToAddAfterStartup.Add( breakPoint );
                  mustBeAddedLater = true;
                }
                //request += "store ";
              }

              if ( !mustBeAddedLater )
              {
                Debug.Log( "Found breakpoint at address " + breakPoint.Address.ToString( "x4" ) );
                breakPointFile += "break $" + breakPoint.Address.ToString( "x4" ) + "\r\n";
                breakPoint.RemoteIndex = remoteIndex;
                ++remoteIndex;

                Document_DocumentEvent( new BaseDocument.DocEvent( BaseDocument.DocEvent.Type.BREAKPOINT_UPDATED, breakPoint ) );
              }
            }
            else
            {
              breakPoint.Address = -1;
              breakPoint.RemoteIndex = -1;

              Document_DocumentEvent( new BaseDocument.DocEvent( BaseDocument.DocEvent.Type.BREAKPOINT_UPDATED, breakPoint ) );
            }
          }
          else
          {
            // manual breakpoint
            string request = "break ";
            bool mustBeAddedOnStartup = false;

            if ( breakPoint.TriggerOnExec )
            {
              request += "exec ";
              mustBeAddedOnStartup = true;
            }
            if ( StudioCore.Debugging.Debugger.m_ViceVersion > RemoteDebugger.WinViceVersion.V_2_3 )
            {
              if ( breakPoint.TriggerOnLoad )
              {
                // store for later addition
                StudioCore.Debugging.BreakpointsToAddAfterStartup.Add( breakPoint );
                //request += "load ";
              }
              if ( breakPoint.TriggerOnStore )
              {
                if ( !StudioCore.Debugging.BreakpointsToAddAfterStartup.Contains( breakPoint ) )
                {
                  // store for later addition
                  StudioCore.Debugging.BreakpointsToAddAfterStartup.Add( breakPoint );
                }
                //request += "store ";
              }
            }
            if ( mustBeAddedOnStartup )
            {
              if ( !string.IsNullOrEmpty( breakPoint.Conditions ) )
              {
                request += breakPoint.Conditions + " ";
              }
              request += "$" + breakPoint.Address.ToString( "x4" );
              breakPointFile += request + "\r\n";
              breakPoint.RemoteIndex = remoteIndex;
              ++remoteIndex;
            }

            Document_DocumentEvent( new BaseDocument.DocEvent( BaseDocument.DocEvent.Type.BREAKPOINT_UPDATED, breakPoint ) );
          }
        }
      }
      string command = toolRun.DebugArguments;

      if ( toolRun.PassLabelsToEmulator )
      {
        breakPointFile += StudioCore.Debugging.DebuggedASMBase.ASMFileInfo.LabelsAsFile();
      }

      if ( breakPointFile.Length > 0 )
      {
        try
        {
          StudioCore.Debugging.TempDebuggerStartupFilename = System.IO.Path.GetTempFileName();
          System.IO.File.WriteAllText( StudioCore.Debugging.TempDebuggerStartupFilename, breakPointFile );
          command += " -moncommands \"" + StudioCore.Debugging.TempDebuggerStartupFilename + "\"";
        }
        catch ( System.IO.IOException ioe )
        {
          System.Windows.Forms.MessageBox.Show( ioe.Message, "Error writing temporary file" );
          StudioCore.AddToOutput( "Error writing temporary file" );
          StudioCore.Debugging.TempDebuggerStartupFilename = "";
          return false;
        }
      }

      Types.CompileTargetType targetType = C64Studio.Types.CompileTargetType.NONE;
      if ( DocumentToRun.Element != null )
      {
        targetType = DocumentToRun.Element.TargetType;
      }

      string  fileToRun = "";
      if ( DocumentToRun.Element != null )
      {
        fileToRun = DocumentToRun.Element.TargetFilename;
        ProjectElement.PerConfigSettings  configSetting = DocumentToRun.Element.Settings[DocumentToRun.Project.Settings.CurrentConfig.Name];
        if ( !string.IsNullOrEmpty( configSetting.DebugFile ) )
        {
          targetType = configSetting.DebugFileType;
        }
      }

      if ( targetType == C64Studio.Types.CompileTargetType.NONE )
      {
        targetType = StudioCore.Compiling.m_LastBuildInfo.TargetType;
      }

        //ParserASM.CompileTarget != Types.CompileTargetType.NONE ) ? ParserASM.CompileTarget : DocumentToRun.Element.TargetType;

      // need to adjust initial breakpoint address for late added store/load breakpoints?
      if ( StudioCore.Debugging.BreakpointsToAddAfterStartup.Count > 0 )
      {
        // yes
        StudioCore.Debugging.LateBreakpointOverrideDebugStart = StudioCore.Debugging.OverrideDebugStart;

        // special start addresses for different run types

        if ( ( targetType == Types.CompileTargetType.CARTRIDGE_MAGICDESK_BIN )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_MAGICDESK_CRT )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_EASYFLASH_BIN )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_EASYFLASH_CRT )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_RGCD_BIN )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_RGCD_CRT )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_16K_BIN )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_16K_CRT )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_8K_BIN )
        ||   ( targetType == Types.CompileTargetType.CARTRIDGE_8K_CRT ) )
        {
          StudioCore.Debugging.OverrideDebugStart = 0x8000;
        }
        else
        {
          // directly after calling load from ram (as VICE does when autostarting a .prg file)
          // TODO - check with .t64, .tap, .d64
          StudioCore.Debugging.OverrideDebugStart = 0xe178;
        }
      }

      if ( StudioCore.Settings.TrueDriveEnabled )
      {
        command = toolRun.TrueDriveOnArguments + " " + command;
      }
      else
      {
        command = toolRun.TrueDriveOffArguments + " " + command;
      }

      RunProcess.StartInfo.Arguments = FillParameters( command, DocumentToRun, true, out error );
      if ( error )
      {
        return false;
      }

      if ( ( targetType == Types.CompileTargetType.CARTRIDGE_MAGICDESK_BIN )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_MAGICDESK_CRT )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_EASYFLASH_BIN )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_EASYFLASH_CRT )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_RGCD_BIN )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_RGCD_CRT )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_16K_BIN )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_16K_CRT )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_8K_BIN )
      ||   ( targetType == Types.CompileTargetType.CARTRIDGE_8K_CRT ) )
      {
        RunProcess.StartInfo.Arguments += " " + FillParameters( toolRun.CartArguments, DocumentToRun, true, out error );
      }
      else
      {
        RunProcess.StartInfo.Arguments += " " + FillParameters( toolRun.PRGArguments, DocumentToRun, true, out error );
      }
      if ( error )
      {
        return false;
      }

      StudioCore.AddToOutput( "Calling " + RunProcess.StartInfo.FileName + " with " + RunProcess.StartInfo.Arguments + System.Environment.NewLine );
      RunProcess.Exited += new EventHandler( runProcess_Exited );

      SetGUIForWaitOnExternalTool( true );
      if ( ( RunProcess.Start() )
      &&   ( RunProcess.WaitForInputIdle() ) )
      {
        // only connect with debugger if VICE
        if ( EmulatorSupportsDebugging( toolRun ) )
        {
          if ( StudioCore.Debugging.Debugger.Connect() )
          {
            m_CurrentActiveTool = toolRun;
            StudioCore.Debugging.DebuggedProject = DocumentToRun.Project;
            AppState = Types.StudioState.DEBUGGING_RUN;
            SetGUIForDebugging( true );
          }
        }
        else
        {
          m_CurrentActiveTool = toolRun;
          StudioCore.Debugging.DebuggedProject = DocumentToRun.Project;
          AppState = Types.StudioState.DEBUGGING_RUN;
          SetGUIForDebugging( true );
        }
      }
      return true;
    }



    /*
    void compilerProcess_Exited( object sender, EventArgs e )
    {
      int     exitCode = CompilerProcess.ExitCode;
      AddToOutput( "Tool Exited with result code " + exitCode.ToString() + System.Environment.NewLine );
      CompilerProcess.Close();
      SetGUIForWaitOnExternalTool( false );

      ProjectConfig   config = m_Project.Settings.Configs[mainToolConfig.SelectedItem.ToString()];

      if ( exitCode != 0 )
      {
        // update errors/warnings
        ParseFile( ParserASM, ActiveDocument, config );
        AppState = State.NORMAL;
        return;
      }

      switch ( AppState )
      {
        case State.BUILD:
          ParseFile( ParserASM, ActiveDocument, config );
          AppState = State.NORMAL;
          break;
        case State.BUILD_AND_RUN:
          // run program
          RunCompiledFile( ActiveDocument, Types.CompileTargetType.NONE );
          break;
        case State.BUILD_AND_DEBUG:
          // run program
          DebugCompiledFile( ActiveDocument );
          break;
        default:
          AppState = State.NORMAL;
          break;
      }
    }
    */



    void runProcess_Exited( object sender, EventArgs e )
    {
      if ( RunProcess == null )
      {
        StudioCore.AddToOutput( "Run exited unexpectedly" + System.Environment.NewLine );
      }
      else
      {
        try
        {
          StudioCore.AddToOutput( "Run exited with result code " + RunProcess.ExitCode + System.Environment.NewLine );
          RunProcess.Close();
          RunProcess.Dispose();
        }
        catch ( System.Exception ex )
        {
          StudioCore.AddToOutput( "Run aborted with error: " + ex.Message + System.Environment.NewLine );
        }
      }
      RunProcess = null;

      StudioCore.Debugging.Debugger.Disconnect();

      if ( StudioCore.Debugging.TempDebuggerStartupFilename.Length > 0 )
      {
        try
        {
          System.IO.File.Delete( StudioCore.Debugging.TempDebuggerStartupFilename );
        }
        catch ( Exception ex )
        {
          StudioCore.AddToOutput( "Failed to delete temporary file " + StudioCore.Debugging.TempDebuggerStartupFilename + ", " + ex.Message );
        }
        StudioCore.Debugging.TempDebuggerStartupFilename = "";
      }

      AppState = Types.StudioState.NORMAL;

      RemoveVirtualBreakpoints();

      SetGUIForDebugging( false );
      SetGUIForWaitOnExternalTool( false );
    }



    private void mainToolSave_Click( object sender, EventArgs e )
    {
      BaseDocument baseDoc = (BaseDocument)panelMain.ActiveDocument;
      if ( baseDoc == null )
      {
        return;
      }
      if ( ( baseDoc.DocumentInfo.Project != null )
      &&   ( baseDoc.DocumentInfo.Project.Modified ) )
      {
        if ( !SaveProject( baseDoc.DocumentInfo.Project ) )
        {
          return;
        }
      }
      baseDoc.Save();
      if ( baseDoc.DocumentInfo.Project != null )
      {
        baseDoc.DocumentInfo.Project.Save( baseDoc.DocumentInfo.Project.Settings.Filename );
      }
    }



    public ProjectElement CreateNewElement( ProjectElement.ElementType Type, string StartName, Project Project )
    {
      if ( Project == null )
      {
        CreateNewDocument( Type, null );
        return null;
      }
      return CreateNewElement( Type, StartName, Project, Project.Node );
    }



    public ProjectElement CreateNewElement( ProjectElement.ElementType Type, string StartName, Project Project, TreeNode ParentNode )
    {
      if ( Project == null )
      {
        return null;
      }
      Project projectToAdd = m_SolutionExplorer.ProjectFromNode( ParentNode );
      ProjectElement elementParent = m_SolutionExplorer.ElementFromNode( ParentNode );

      ProjectElement element    = projectToAdd.CreateElement( Type, ParentNode );
      element.Name              = StartName;
      element.Node.Text         = StartName;
      element.ProjectHierarchy  = m_SolutionExplorer.GetElementHierarchy( element.Node );
      element.DocumentInfo.Project = Project;

      if ( element.Document != null )
      {
        RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.DOCUMENT_CREATED, element.DocumentInfo ) );
      }
      projectToAdd.ShowDocument( element );
      if ( element.Document != null )
      {
        element.Document.SetModified();
      }
      RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.DOCUMENT_INFO_CREATED, element.DocumentInfo ) );
      return element;
    }



    public void AddBreakpoint( Types.Breakpoint Breakpoint )
    {
      if ( AppState == Types.StudioState.NORMAL )
      {
        if ( !StudioCore.Debugging.BreakPoints.ContainsKey( Breakpoint.DocumentFilename ) )
        {
          StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename] = new List<C64Studio.Types.Breakpoint>();
        }
        StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename].Add( Breakpoint );
        //Debug.Log( "add breakpoint for " + asm.DocumentFilename + " at line " + Breakpoint.LineIndex );
      }
      else if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      {
        if ( !StudioCore.Debugging.BreakPoints.ContainsKey( Breakpoint.DocumentFilename ) )
        {
          StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename] = new List<C64Studio.Types.Breakpoint>();
        }
        StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename].Add( Breakpoint );
        StudioCore.Debugging.Debugger.AddBreakpoint( Breakpoint );
        //Debug.Log( "add live breakpoint for " + asm.DocumentFilename + " at line " + Breakpoint.LineIndex );
      }
      else
      {
        return;
      }
      m_DebugBreakpoints.AddBreakpoint( Breakpoint );
    }



    private void RemoveBreakpoint( Types.Breakpoint Breakpoint )
    {
      if ( AppState == Types.StudioState.NORMAL )
      {
        if ( StudioCore.Debugging.BreakPoints.ContainsKey( Breakpoint.DocumentFilename ) )
        {
          foreach ( Types.Breakpoint breakPoint in StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename] )
          {
            if ( breakPoint == Breakpoint )
            {
              StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename].Remove( breakPoint );
              m_DebugBreakpoints.RemoveBreakpoint( breakPoint );
              StudioCore.Debugging.Debugger.RemoveBreakpoint( breakPoint.RemoteIndex );
              Debug.Log( "-removed" );
              break;
            }
          }
        }

        if ( Breakpoint.DocumentFilename != "C64Studio.DebugBreakpoints" )
        {
          ProjectElement    element = CurrentProject.GetElementByFilename( Breakpoint.DocumentFilename );
          if ( ( element != null )
          &&   ( element.Document != null )
          &&   ( element.Document is SourceASMEx ) )
          {
            SourceASMEx asm = (SourceASMEx)element.Document;
            asm.RemoveBreakpoint( Breakpoint );

            Debug.Log( "remove breakpoint for " + asm.DocumentFilename + " at line " + Breakpoint.LineIndex );
          }
        }
      }
      else if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      {
        //Debug.Log( "try to remove live breakpoint for " + Event.Doc.DocumentFilename + " at line " + Event.LineIndex );
        if ( StudioCore.Debugging.BreakPoints.ContainsKey( Breakpoint.DocumentFilename ) )
        {
          foreach ( Types.Breakpoint breakPoint in StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename] )
          {
            if ( breakPoint == Breakpoint )
            {
              StudioCore.Debugging.BreakPoints[Breakpoint.DocumentFilename].Remove( breakPoint );
              m_DebugBreakpoints.RemoveBreakpoint( breakPoint );

              StudioCore.Debugging.Debugger.RemoveBreakpoint( breakPoint.RemoteIndex, breakPoint );
              //Debug.Log( "-removed" );
              break;
            }
          }
        }
      }
    }



    public void Document_DocumentEvent( BaseDocument.DocEvent Event )
    {
      if ( InvokeRequired )
      {
        Invoke( new DocumentEventHandlerCallback( Document_DocumentEvent ), new object[] { Event } );
        return;
      }

      switch ( Event.EventType )
      {
        case BaseDocument.DocEvent.Type.BREAKPOINT_ADDED:
          if ( ( AppState == Types.StudioState.NORMAL )
          ||   ( AppState == Types.StudioState.DEBUGGING_BROKEN ) )
          {
            AddBreakpoint( Event.Breakpoint );
          }
          break;
        case BaseDocument.DocEvent.Type.BREAKPOINT_REMOVED:
          if ( ( AppState == Types.StudioState.NORMAL )
          ||   ( AppState == Types.StudioState.DEBUGGING_BROKEN ) )
          {
            RemoveBreakpoint( Event.Breakpoint );
          }
          break;
        case BaseDocument.DocEvent.Type.BREAKPOINT_UPDATED:
          // address changed
          m_DebugBreakpoints.UpdateBreakpoint( Event.Breakpoint );
          break;
      }
    }



    private void AddNewDocumentOrElement( ProjectElement.ElementType Type, string Description, Project ParentProject, TreeNode ParentNode )
    {
      if ( ParentProject != null )
      {
        var dialogResult = System.Windows.Forms.MessageBox.Show( "Add the new document to the current project?\r\nIf you choose no, the document will be created not as part of the current project.", "Add to current project?", System.Windows.Forms.MessageBoxButtons.YesNoCancel );
        if ( dialogResult == DialogResult.Cancel )
        {
          return;
        }
        if ( dialogResult == DialogResult.Yes )
        {
          AddNewElement( Type, Description, ParentProject, ParentNode );
          return;
        }
        // fall through
      }

      // project-less doc

      string newFilename;
      if ( !ChooseFilename( Type, Description, ParentProject, out newFilename ) )
      {
        return;
      }

      if ( System.IO.File.Exists( newFilename ) )
      {
        var result = System.Windows.Forms.MessageBox.Show( "There is already an existing file at " + newFilename + ".\r\nDo you want to overwrite it?", "Overwrite existing file?", MessageBoxButtons.YesNo );
        if ( result == DialogResult.No )
        {
          return;
        }
      }
      var doc = CreateNewDocument( Type, null );
      doc.SetDocumentFilename( newFilename );
      doc.SetModified();
    }



    private void mainToolNewASMFile_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.ASM_SOURCE, "ASM File", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void mainToolNewItem_ButtonClick( object sender, EventArgs e )
    {
      if ( m_CurrentProject == null )
      {
        mainToolNewProject_Click( sender, e );
        return;
      }
    }



    public Project NewProjectWizard( string ProjectName )
    {
      FormProjectWizard projectWizard = new FormProjectWizard( ProjectName, StudioCore.Settings );

      if ( projectWizard.ShowDialog() != DialogResult.OK )
      {
        return null;
      }
      Project newProject = new Project();
      newProject.Core = StudioCore;
      newProject.Settings.Name = projectWizard.ProjectName;
      newProject.Settings.Filename = projectWizard.ProjectFilename;
      newProject.Settings.BasePath = System.IO.Path.GetDirectoryName( newProject.Settings.Filename );
      newProject.Node = new TreeNode();
      newProject.Node.Tag = newProject;
      newProject.Node.Text = newProject.Settings.Name;


      try
      {
        System.IO.Directory.CreateDirectory( projectWizard.ProjectPath );
      }
      catch ( System.Exception e )
      {
        System.Windows.Forms.MessageBox.Show( "Could not create project folder:" + System.Environment.NewLine + e.Message, "Could not create project folder" );
        return null;
      }

      Text += " - " + newProject.Settings.Name;

      // TODO - adjust GUI to changed project
      if ( m_Solution == null )
      {
        m_Solution = new Solution( this );
      }
      m_Solution.Projects.Add( newProject );

      // TODO - should be different
      m_SolutionExplorer.treeProject.Nodes.Add( newProject.Node );

      RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.SOLUTION_OPENED ) );

      SetActiveProject( newProject );

      SaveProject( newProject );
      UpdateUndoSettings();
      return newProject;
    }



    public bool CreateNewProject()
    {
      if ( !CloseAllProjects() )
      {
        return false;
      }
      return ( AddNewProject() != null );
    }



    public Project AddNewProject()
    {
      string    projectName = "New Project";
      Project newProject = NewProjectWizard( projectName );
      if ( newProject == null )
      {
        return null;
      }
      projectToolStripMenuItem.Visible = true;

      //m_ProjectExplorer.NodeProject.Text  = newProject.Settings.Name;
      //m_ProjectExplorer.NodeProject.Tag   = newProject;

      foreach ( string configName in newProject.Settings.Configs.Keys )
      {
        mainToolConfig.Items.Add( configName );
        if ( ( newProject.Settings.CurrentConfig != null )
        &&   ( configName == newProject.Settings.CurrentConfig.Name ) )
        {
          mainToolConfig.SelectedItem = configName;
        }
      }
      if ( mainToolConfig.SelectedItem == null )
      {
        mainToolConfig.SelectedItem = "Default";
      }
      return newProject;
    }



    private void mainToolNewProject_Click( object sender, EventArgs e )
    {
      CreateNewProject();
    }



    public bool CloseProject( Project ProjectToClose )
    {
      if ( ProjectToClose == null )
      {
        return true;
      }
      if ( ProjectToClose.Modified )
      {
        System.Windows.Forms.DialogResult saveResult = System.Windows.Forms.MessageBox.Show( "The project " + ProjectToClose.Settings.Name + " has been modified. Do you want to save the changes now?", "Save Changes?", MessageBoxButtons.YesNoCancel );

        if ( saveResult == DialogResult.Yes )
        {
          if ( !SaveProject( ProjectToClose ) )
          {
            return false;
          }
        }
        else if ( saveResult == DialogResult.Cancel )
        {
          return false;
        }
      }
      /*
      if ( m_Project.Settings.Filename == null )
      {
        return false;
      }
       */
      bool changes = false;
      foreach ( ProjectElement element in ProjectToClose.Elements )
      {
        if ( element.Document != null )
        {
          if ( element.Document.Modified )
          {
            changes = true;
            break;
          }
        }
      }

      if ( changes )
      {
        DialogResult res = System.Windows.Forms.MessageBox.Show( "There are changes in one or more items. Do you want to save them before closing?", "Unsaved changes, save now?", MessageBoxButtons.YesNoCancel );
        if ( res == DialogResult.Cancel )
        {
          return false;
        }
        if ( res == DialogResult.Yes )
        {
          foreach ( ProjectElement element in ProjectToClose.Elements )
          {
            if ( element.Document != null )
            {
              element.Document.Save();
            }
          }
        }
      }

      foreach ( ProjectElement element in ProjectToClose.Elements )
      {
        if ( element.Document != null )
        {
          element.Document.ForceClose();
        }

        RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.DOCUMENT_INFO_REMOVED, element.DocumentInfo ) );
      }

      try
      {
        m_SolutionExplorer.treeProject.Nodes.Remove( ProjectToClose.Node );
      }
      catch
      {
      }

      // TODO - adjust GUI to changed project
      m_Solution.RemoveProject( ProjectToClose );
      if ( m_CurrentProject == ProjectToClose )
      {
        mainToolConfig.Items.Clear();
        m_CurrentProject = null;
      }

      projectToolStripMenuItem.Visible = false;
      StudioCore.Debugging.BreakPoints.Clear();
      return true;
    }



    public string FilterString( string Source )
    {
      return Source.Substring( 0, Source.Length - 1 );
    }



    private bool SaveProject( Project ProjectToSave )
    {
      if ( ProjectToSave == null )
      {
        return false;
      }
      if ( ProjectToSave.Settings.Filename == null )
      {
        System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

        saveDlg.Title = "Save Project as";
        saveDlg.Filter = FilterString( Types.Constants.FILEFILTER_PROJECT + Types.Constants.FILEFILTER_ALL );
        if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
        {
          return false;
        }
        ProjectToSave.Settings.Filename = saveDlg.FileName;
        ProjectToSave.Settings.BasePath = System.IO.Path.GetDirectoryName( saveDlg.FileName );
      }
      if ( !ProjectToSave.Save( ProjectToSave.Settings.Filename ) )
      {
        return false;
      }
      //Settings.UpdateInMRU( ProjectToSave.Settings.Filename, this );
      return true;
    }



    private void closeProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( m_CurrentProject != null )
      {
        CloseProject( m_CurrentProject );
      }
    }



    private void saveProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      SaveProject( m_CurrentProject );
    }



    public void SetActiveProject( Project NewProject )
    {
      if ( m_LoadingProject )
      {
        return;
      }
      if ( m_CurrentProject != NewProject )
      {
        m_CurrentProject = NewProject;
        RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent(C64Studio.Types.ApplicationEvent.Type.ACTIVE_PROJECT_CHANGED, NewProject ) );
        if ( mainToolConfig.ComboBox != null )
        {
          mainToolConfig.Items.Clear();
          if ( NewProject != null )
          {
            foreach ( string configName in NewProject.Settings.Configs.Keys )
            {
              mainToolConfig.Items.Add( configName );
              if ( ( NewProject.Settings.CurrentConfig != null )
              &&   ( configName == NewProject.Settings.CurrentConfig.Name ) )
              {
                mainToolConfig.SelectedItem = configName;
              }
            }
            if ( mainToolConfig.SelectedItem == null )
            {
              mainToolConfig.SelectedItem = "Default";
            }
          }
        }
      }
    }



    public Project OpenProject( string Filename )
    {
      if ( m_Solution != null )
      {
        foreach ( Project project in m_Solution.Projects )
        {
          if ( GR.Path.IsPathEqual( Filename, project.Settings.Filename ) )
          {
            System.Windows.Forms.MessageBox.Show( "The project " + Filename + " is already opened in this solution.", "Project already opened" );
            return null;
          }
        }
      }

      bool    createdNewSolution = false;
      if ( m_Solution == null )
      {
        createdNewSolution = true;
        m_Solution = new Solution( this );

        SaveSolution();
      }

      Project newProject = new Project();
      newProject.Core = StudioCore;
      m_LoadingProject = true;
      if ( newProject.Load( Filename ) )
      {
        m_LoadingProject = false;
        m_Solution.Projects.Add( newProject );
        m_SolutionExplorer.treeProject.Nodes.Add( newProject.Node );
        projectToolStripMenuItem.Visible = true;
        //Settings.UpdateInMRU( newProject.Settings.Filename, this );

        if ( createdNewSolution )
        {
          RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.SOLUTION_OPENED ) );
        }

        SetActiveProject( newProject );

        List<string>        updatedFiles = new List<string>();

        List<ProjectElement>    elementsToPreParse = new List<ProjectElement>( newProject.Elements );

        try
        {
          // check if main doc is set, parse this one first as it's likely to include most other files
          if ( !string.IsNullOrEmpty( newProject.Settings.MainDocument ) )
          {
            ProjectElement          mainElement = newProject.GetElementByFilename( newProject.Settings.MainDocument );

            if ( mainElement != null )
            {
              elementsToPreParse.Remove( mainElement );
              elementsToPreParse.Insert( 0, mainElement );
            }
          }


          foreach ( ProjectElement element in elementsToPreParse )
          {
            if ( element.Document == null )
            {
              continue;
            }
            if ( element.DocumentInfo.Type == ProjectElement.ElementType.ASM_SOURCE )
            {
              if ( updatedFiles.Contains( element.DocumentInfo.FullPath ) )
              {
                // do not reparse already parsed element
                continue;
              }
              ParseFile( StudioCore.Compiling.ParserASM, element.DocumentInfo, newProject.Settings.Configs[mainToolConfig.SelectedItem.ToString()] );

              //var knownTokens = ParserASM.KnownTokens();
              //var knownTokenInfos = ParserASM.KnownTokenInfo();
              //Debug.Log( "SetASMFileInfo on " + element.DocumentInfo.DocumentFilename );
              //element.DocumentInfo.SetASMFileInfo( ParserASM.ASMFileInfo, knownTokens, knownTokenInfos );

              updatedFiles.Add( element.DocumentInfo.FullPath );

              ( (SourceASMEx)element.Document ).SetLineInfos( StudioCore.Compiling.ParserASM.ASMFileInfo );
              ( (SourceASMEx)element.Document ).OnKnownKeywordsChanged();
              ( (SourceASMEx)element.Document ).OnKnownTokensChanged();

              foreach ( var dependencyBuildState in element.DocumentInfo.DeducedDependency.Values )
              {
                foreach ( var dependency in dependencyBuildState.BuildState.Keys )
                {
                  ProjectElement    element2 = newProject.GetElementByFilename( dependency );
                  if ( ( element2 != null )
                  && ( element2.DocumentInfo.Type == ProjectElement.ElementType.ASM_SOURCE ) )
                  {
                    if ( element2.Document != null )
                    {
                      ( (SourceASMEx)element2.Document ).SetLineInfos( StudioCore.Compiling.ParserASM.ASMFileInfo );
                    }
                    updatedFiles.Add( element2.DocumentInfo.FullPath );
                  }
                }
              }
            }
          }
          m_CompileResult.ClearMessages();
          if ( m_ActiveSource != null )
          {
            //Debug.Log( "RefreshFromDocument after openproject" );
            m_Outline.RefreshFromDocument( m_ActiveSource );
          }
          UpdateCaption();
          SaveSolution();
        }
        catch ( Exception ex )
        {
          StudioCore.AddToOutput( "An error occurred during opening and preparsing the project\r\n" + ex.ToString() );
        }
        return newProject;
      }
      m_LoadingProject = false;
      return null;
    }



    private void UpdateCaption()
    {
      if ( CurrentProject != null )
      {
        Text = "C64Studio - " + CurrentProject.Settings.Name;
      }
      else
      {
        Text = "C64Studio";
      }
    }



    private void projectOpenToolStripMenuItem_Click( object sender, EventArgs e )
    {
      CloseSolution();

      System.Windows.Forms.OpenFileDialog openDlg = new System.Windows.Forms.OpenFileDialog();
      openDlg.Title = "Open solution or project";
      openDlg.Filter = FilterString( Types.Constants.FILEFILTER_SOLUTION_OR_PROJECTS + Types.Constants.FILEFILTER_SOLUTION + Types.Constants.FILEFILTER_PROJECT + Types.Constants.FILEFILTER_ALL );
      if ( openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return;
      }
      string extension = System.IO.Path.GetExtension( openDlg.FileName ).ToUpper();
      if ( extension == ".S64" )
      {
        OpenSolution( openDlg.FileName );
      }
      else if ( extension == ".C64" )
      {
        OpenProject( openDlg.FileName );
      }
    }



    private void editorOpenToolStripMenuItem_Click( object sender, EventArgs e )
    {
      System.Windows.Forms.OpenFileDialog openDlg = new System.Windows.Forms.OpenFileDialog();
      openDlg.Title = "Open editor file";
      openDlg.Filter = FilterString( Types.Constants.FILEFILTER_CHARSET_SCREEN + Types.Constants.FILEFILTER_GRAPHIC_SCREEN + Types.Constants.FILEFILTER_ALL );
      if ( openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return;
      }
      OpenFile( openDlg.FileName );
    }

    
    
    private void BuildAndRun( DocumentInfo DocumentToBuild, DocumentInfo DocumentToRun )
    {
      if ( AppState != Types.StudioState.NORMAL )
      {
        return;
      }
      AppState = Types.StudioState.BUILD_AND_RUN;
      StudioCore.Debugging.OverrideDebugStart = -1;
      if ( !StartCompile( DocumentToBuild, null, DocumentToRun ) )
      {
        AppState = Types.StudioState.NORMAL;
      }
    }



    private void mainToolCompileAndRun_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.BUILD_AND_RUN );
    }



    public void MarkLine( Project MarkProject, string DocumentFilename, int Line )
    {
      if ( InvokeRequired )
      {
        Invoke( new Navigating.OpenDocumentAndGotoLineCallback( MarkLine ), new object[] { MarkProject, DocumentFilename, Line } );
        return;
      }
      if ( StudioCore.Debugging.MarkedDocument != null )
      {
        StudioCore.Debugging.MarkedDocument.SetLineMarked( StudioCore.Debugging.MarkedDocumentLine, false );
      }
      string  inPath = DocumentFilename.Replace( "\\", "/" );
      if ( MarkProject != null )
      {
        foreach ( ProjectElement element in MarkProject.Elements )
        {
          string myPath = GR.Path.Append( MarkProject.Settings.BasePath, element.Filename ).Replace( "\\", "/" );
          if ( String.Compare( myPath, inPath, true ) == 0 )
          {
            BaseDocument doc = MarkProject.ShowDocument( element );
            StudioCore.Debugging.MarkedDocument = doc;
            StudioCore.Debugging.MarkedDocumentLine = Line;
            if ( doc != null )
            {
              doc.SetLineMarked( Line, Line != -1 );
            }
            return;
          }
        }
      }
      foreach ( IDockContent dockContent in panelMain.Documents )
      {
        BaseDocument baseDoc = (BaseDocument)dockContent;
        if ( baseDoc.DocumentFilename == null )
        {
          continue;
        }

        string myPath = baseDoc.DocumentFilename.Replace( "\\", "/" );
        if ( String.Compare( myPath, inPath, true ) == 0 )
        {
          StudioCore.Debugging.MarkedDocument = baseDoc;
          StudioCore.Debugging.MarkedDocumentLine = Line;
          baseDoc.Select();
          baseDoc.SetLineMarked( Line, Line != -1 );
          return;
        }
      }
    }



    public void ProjectConfigChanged()
    {
      if ( m_CurrentProject != null )
      {
        foreach ( var element in m_CurrentProject.Elements )
        {
          element.DocumentInfo.HasBeenSuccessfullyBuilt = false;
        }
      }
      foreach ( IDockContent dockContent in panelMain.Documents )
      {
        BaseDocument baseDoc = (BaseDocument)dockContent;

        baseDoc.DocumentInfo.HasBeenSuccessfullyBuilt = false;
      }
    }



    public bool ImportExistingFiles( TreeNode Node )
    {
      Project projectToAddTo = null;
      if ( Node != null )
      {
        projectToAddTo = m_SolutionExplorer.ProjectFromNode( Node );
      }
      if ( projectToAddTo == null )
      {
        projectToAddTo = m_CurrentProject;
        Node = projectToAddTo.Node;
      }
      if ( projectToAddTo == null )
      {
        return false;
      }

      if ( !SaveProject( projectToAddTo ) )
      {
        return false;
      }

      System.Windows.Forms.OpenFileDialog openDlg = new System.Windows.Forms.OpenFileDialog();
      openDlg.Title = "Open existing item";
      openDlg.Filter = FilterString( Types.Constants.FILEFILTER_ALL_SUPPORTED_FILES + Types.Constants.FILEFILTER_ASM + Types.Constants.FILEFILTER_CHARSET + Types.Constants.FILEFILTER_SPRITE + Types.Constants.FILEFILTER_BASIC + Types.Constants.FILEFILTER_BINARY_FILES + Types.Constants.FILEFILTER_ALL );
      openDlg.InitialDirectory = projectToAddTo.Settings.BasePath;
      openDlg.Multiselect = true;
      if ( openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return false;
      }

      foreach ( var fileName in openDlg.FileNames )
      {
        string    importFile = fileName;

        bool      skipFile = false;

        if ( projectToAddTo.IsFilenameInUse( importFile ) )
        {
          System.Windows.Forms.MessageBox.Show( "File " + importFile + " is already part of this project", "File already added" );
          skipFile = true;
          break;
        }
        if ( skipFile )
        {
          continue;
        }

        // determine type by extension
        ProjectElement.ElementType type = ProjectElement.ElementType.ASM_SOURCE;
        string newFileExtension = System.IO.Path.GetExtension( GR.Path.RelativePathTo( importFile, false, System.IO.Path.GetFullPath( projectToAddTo.Settings.BasePath ), true ).ToUpper() );

        if ( ( newFileExtension == ".CHARSETPROJECT" )
        || ( newFileExtension == ".CHR" ) )
        {
          type = ProjectElement.ElementType.CHARACTER_SET;
        }
        else if ( ( newFileExtension == ".SPRITEPROJECT" )
        ||        ( newFileExtension == ".SPR" ) )
        {
          type = ProjectElement.ElementType.SPRITE_SET;
        }
        else if ( newFileExtension == ".BIN" )
        {
          type = ProjectElement.ElementType.BINARY_FILE;
        }
        else if ( newFileExtension == ".CHARSCREEN" )
        {
          type = ProjectElement.ElementType.CHARACTER_SCREEN;
        }
        else if ( newFileExtension == ".GRAPHICSCREEN" )
        {
          type = ProjectElement.ElementType.GRAPHIC_SCREEN;
        }
        else if ( newFileExtension == ".BAS" )
        {
          type = ProjectElement.ElementType.BASIC_SOURCE;
        }
        else if ( newFileExtension == ".MAPPROJECT" )
        {
          type = ProjectElement.ElementType.MAP_EDITOR;
        }

        if ( !GR.Path.IsSubPath( System.IO.Path.GetFullPath( projectToAddTo.Settings.BasePath ), importFile ) )
        {
          // not a sub folder
          var result = System.Windows.Forms.MessageBox.Show( "The item is not inside the current project folder. Should a copy be created in the project folder?",
                                                             "Create a local copy of the added file?",
                                                             MessageBoxButtons.YesNoCancel );
          if ( result == DialogResult.Cancel )
          {
            return false;
          }
          if ( result == DialogResult.Yes )
          {
            // create a copy
            string pureFileName = System.IO.Path.GetFileName( importFile );
            string newFileName = System.IO.Path.Combine( System.IO.Path.GetFullPath( projectToAddTo.Settings.BasePath ), pureFileName );

            while ( System.IO.File.Exists( newFileName ) )
            {
              newFileName = System.IO.Path.Combine( System.IO.Path.GetFullPath( projectToAddTo.Settings.BasePath ), System.IO.Path.GetFileNameWithoutExtension( newFileName ) + "_" + System.IO.Path.GetExtension( newFileName ) );
            }
            try
            {
              System.IO.File.Copy( importFile, newFileName, false );
            }
            catch ( System.Exception ex )
            {
              StudioCore.AddToOutput( "Could not copy file to new location: " + ex.Message );
              return false;
            }
            importFile = newFileName;
          }
        }

        TreeNode    parentNodeToInsertTo = Node;

        ProjectElement element = projectToAddTo.CreateElement( type, parentNodeToInsertTo );

        //string    relativeFilename = GR.Path.RelativePathTo( openDlg.FileName, false, System.IO.Path.GetFullPath( m_Project.Settings.BasePath ), true );
        string relativeFilename = GR.Path.RelativePathTo( System.IO.Path.GetFullPath( projectToAddTo.Settings.BasePath ), true, importFile, false );
        element.Name = System.IO.Path.GetFileNameWithoutExtension( relativeFilename );
        element.Filename = relativeFilename;

        while ( parentNodeToInsertTo.Level >= 1 )
        {
          element.ProjectHierarchy.Insert( 0, parentNodeToInsertTo.Text );
          parentNodeToInsertTo = parentNodeToInsertTo.Parent;
        }
        projectToAddTo.ShowDocument( element );
        element.DocumentInfo.DocumentFilename = relativeFilename;
        if ( element.Document != null )
        {
          element.Document.SetDocumentFilename( relativeFilename );
        }
        projectToAddTo.SetModified();
      }
      return true;
    }



    private void importFileToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ImportExistingFiles( null );
    }



    private void SetGUIForWaitOnExternalTool( bool Wait )
    {
      if ( InvokeRequired )
      {
        Invoke( new SetGUIForWaitOnExternalToolCallback( SetGUIForWaitOnExternalTool ), new object[] { Wait } );
      }
      else
      {
        mainTools.Enabled = !Wait;
      }
    }



    private void SetGUIForDebugging( bool DebugModeActive )
    {
      if ( InvokeRequired )
      {
        Invoke( new SetGUIForWaitOnExternalToolCallback( SetGUIForDebugging ), new object[] { DebugModeActive } );
      }
      else
      {
        try
        {
          debugTools.Enabled = DebugModeActive;
          menuWindowToolbarDebugger.Checked = debugTools.Enabled;
          if ( DebugModeActive )
          {
            SetToolPerspective( Perspective.DEBUG );
            /*
            m_DebugBreakpoints.Show( panelMain );
            breakpointsToolStripMenuItem.Checked = true;
            Settings.Tools[m_DebugBreakpoints.Text].Visible = true;

            m_DebugWatch.Show( panelMain );
            debugWatchToolStripMenuItem.Checked = true;
            Settings.Tools[m_DebugWatch.Text].Visible = true;

            m_DebugMemory.Show( panelMain );
            debugMemoryToolStripMenuItem.Checked = true;
            Settings.Tools[m_DebugMemory.Text].Visible = true;

            m_DebugRegisters.Show( panelMain );
            debugRegistersToolStripMenuItem.Checked = true;
            Settings.Tools[m_DebugRegisters.Text].Visible = true;
            */

            mainDebugGo.Enabled = ( AppState != Types.StudioState.DEBUGGING_RUN );
            mainDebugBreak.Enabled = ( AppState == Types.StudioState.DEBUGGING_RUN );
          }
          else
          {
            SetToolPerspective( Perspective.EDIT );

            /*
            m_DebugBreakpoints.Hide();
            breakpointsToolStripMenuItem.Checked = false;
            Settings.Tools[m_DebugBreakpoints.Text].Visible = false;

            m_DebugRegisters.Hide();
            debugRegistersToolStripMenuItem.Checked = false;
            Settings.Tools[m_DebugRegisters.Text].Visible = false;

            m_DebugWatch.Hide();
            debugWatchToolStripMenuItem.Checked = false;
            Settings.Tools[m_DebugWatch.Text].Visible = false;

            m_DebugMemory.Hide();
            debugMemoryToolStripMenuItem.Checked = false;
            Settings.Tools[m_DebugMemory.Text].Visible = false;
            */

            mainDebugGo.Enabled = true;
          }
        }
        catch ( NullReferenceException )
        {
          // may happen during shutdown
        }
      }
    }



    private void debugConnectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( !StudioCore.Debugging.Debugger.Connect() )
      {
        Debug.Log( "Connect failed" );
      }
    }



    private void debugDisconnectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      StudioCore.Debugging.Debugger.Disconnect();
    }


    
    private void filePreferencesToolStripMenuItem_Click( object sender, EventArgs e )
    {
      Settings prefDlg = new Settings( StudioCore, C64Studio.Settings.TabPage.GENERAL );

      prefDlg.ShowDialog();
    }



    private void SaveSettings()
    {
      StudioCore.Settings.MainWindowPlacement = GR.Forms.WindowStateManager.GeometryToString( this );

      m_FindReplace.ToSettings( StudioCore.Settings );

      GR.Memory.ByteBuffer SettingsData = StudioCore.Settings.ToBuffer();

      string    settingFilename = System.IO.Path.Combine( Application.UserAppDataPath, "settings.dat" );

      System.IO.Directory.CreateDirectory( System.IO.Directory.GetParent( settingFilename ).FullName );
      System.IO.File.WriteAllBytes( settingFilename, SettingsData.Data() );

      CloseSolution();
    }



    private bool LoadSettings()
    {
      string    SettingFile = System.IO.Path.Combine( Application.UserAppDataPath, "settings.dat" );

      GR.Memory.ByteBuffer    SettingsData = null;
      try
      {
        SettingsData = new GR.Memory.ByteBuffer( System.IO.File.ReadAllBytes( SettingFile ) );
      }
      catch ( System.IO.FileNotFoundException )
      {
        return false;
      }

      if ( SettingsData.Empty() )
      {
        return false;
      }

      if ( !StudioCore.Settings.ReadFromBuffer( SettingsData ) )
      {
        return false;
      }

      if ( StudioCore.Settings.SyntaxColoring.Count == 0 )
      {
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.NONE, new C64Studio.Types.ColorSetting( "Common Code", 0xff000000 ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.CODE, new C64Studio.Types.ColorSetting( "Code", 0xff000000 ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.LITERAL_STRING, new C64Studio.Types.ColorSetting( "String Literal", 0xff800000 ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.LITERAL_NUMBER, new C64Studio.Types.ColorSetting( "Numeric Literal", 0xff0000ff ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.LABEL, new C64Studio.Types.ColorSetting( "Label", 0xff800000 ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.COMMENT, new C64Studio.Types.ColorSetting( "Comment", 0xff008000 ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.PSEUDO_OP, new C64Studio.Types.ColorSetting( "Macro", 0xffffff00 ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.CURRENT_DEBUG_LINE, new C64Studio.Types.ColorSetting( "Current Debug Line", 0xff000000, 0xffffff00 ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.EMPTY_SPACE, new C64Studio.Types.ColorSetting( "Macro", 0xff000000, 0xffffffff ) );
        StudioCore.Settings.SyntaxColoring.Add( C64Studio.Types.ColorableElement.OPERATOR, new C64Studio.Types.ColorSetting( "Macro", 0xff000000 ) );
      }
      return true;
    }



    private void MainForm_FormClosed( object sender, FormClosedEventArgs e )
    {
      StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.QUIT );
    }



    private void BuildAndDebug( DocumentInfo DocumentToBuild, DocumentInfo DocumentToDebug, DocumentInfo DocumentToRun )
    {
      if ( AppState != Types.StudioState.NORMAL )
      {
        return;
      }
      AppState = Types.StudioState.BUILD_AND_DEBUG;
      StudioCore.Debugging.OverrideDebugStart = -1;
      if ( !StartCompile( DocumentToBuild, DocumentToDebug, DocumentToRun ) )
      {
        AppState = Types.StudioState.NORMAL;
      }
    }



    private void mainToolDebug_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.BUILD_AND_DEBUG );
    }



    public void RunToAddress( DocumentInfo DocumentToDebug, DocumentInfo DocumentToRun, int DebugAddress )
    {
      if ( AppState == Types.StudioState.NORMAL )
      {
        StartDebugAt( DocumentToDebug, DocumentToRun, DebugAddress );
      }
      else if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      {
        // unmark current marked line
        if ( StudioCore.Debugging.MarkedDocument != null )
        {
          MarkLine( StudioCore.Debugging.MarkedDocument.DocumentInfo.Project, StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, -1 );
          StudioCore.Debugging.MarkedDocument = null;
        }

        if ( ( RunProcess != null )
        &&   ( RunProcess.MainWindowHandle != IntPtr.Zero ) )
        {
          SetForegroundWindow( RunProcess.MainWindowHandle );
        }

        AppState = Types.StudioState.DEBUGGING_RUN;
        StudioCore.Debugging.FirstActionAfterBreak = false;
        mainDebugGo.Enabled = false;
        mainDebugBreak.Enabled = true;

        Types.Breakpoint    tempBP = new C64Studio.Types.Breakpoint();
        tempBP.Address = DebugAddress;
        tempBP.Temporary = true;
        Debug.Log( "Try to add Breakpoint at $" + DebugAddress.ToString( "X4" ) );
        StudioCore.Debugging.Debugger.AddBreakpoint( tempBP );
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.EXIT );
      }
    }



    private void StartDebugAt( DocumentInfo DocumentToDebug, DocumentInfo DocumentToRun, int DebugAddress )
    {
      if ( AppState != Types.StudioState.NORMAL )
      {
        return;
      }

      if ( InvokeRequired )
      {
        Invoke( new StartDebugAtCallback( StartDebugAt ), new object[] { DebugAddress } );
      }
      else
      {
        AppState = Types.StudioState.BUILD_AND_DEBUG;
        StudioCore.Debugging.OverrideDebugStart = DebugAddress;
        if ( !StartCompile( DocumentToRun, DocumentToDebug, DocumentToRun ) )
        {
          AppState = Types.StudioState.NORMAL;
        }
      }
    }



    public void ForceEmulatorRefresh()
    {
      if ( RunProcess != null )
      {
        try
        {
          // hack that's needed for WinVICE to continue
          // fixed in WinVICE r25309
          InvalidateRect( RunProcess.MainWindowHandle, IntPtr.Zero, false );
        }
        catch ( System.InvalidOperationException )
        {
        }
      }
    }



    private void DebugGo()
    {
      if ( ( m_CurrentActiveTool != null )
      &&   ( !EmulatorSupportsDebugging( m_CurrentActiveTool ) ) )
      {
        return;
      }

      if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      {
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.EXIT );

        if ( StudioCore.Debugging.MarkedDocument != null )
        {
          MarkLine( StudioCore.Debugging.MarkedDocument.DocumentInfo.Project, StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, -1 );
          StudioCore.Debugging.MarkedDocument = null;
        }

        if ( ( RunProcess != null )
        &&   ( RunProcess.MainWindowHandle != IntPtr.Zero ) )
        {
          SetForegroundWindow( RunProcess.MainWindowHandle );
        }

        AppState = Types.StudioState.DEBUGGING_RUN;
        StudioCore.Debugging.FirstActionAfterBreak = false;
        mainDebugGo.Enabled = false;
        mainDebugBreak.Enabled = true;
      }
    }



    private void mainDebugGo_Click( object sender, EventArgs e )
    {
      DebugGo();
    }



    private void DebugBreak()
    {
      if ( ( m_CurrentActiveTool != null )
      &&   ( !EmulatorSupportsDebugging( m_CurrentActiveTool ) ) )
      {
        return;
      }

      if ( AppState == Types.StudioState.DEBUGGING_RUN )
      {
        // send any command to break into the monitor again
        try
        {
          StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.NEXT );
          StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_VALUES );
          StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_MEMORY, m_DebugMemory.MemoryStart, m_DebugMemory.MemorySize );

          SetForegroundWindow( this.Handle );
        }
        catch ( Exception ex )
        {
          StudioCore.AddToOutput( "Exception while debug break:" + ex.ToString() );
        }

        AppState = Types.StudioState.DEBUGGING_BROKEN;
        StudioCore.Debugging.FirstActionAfterBreak = true;
        mainDebugGo.Enabled = true;
        mainDebugBreak.Enabled = false;
      }
    }



    private void mainDebugBreak_Click( object sender, EventArgs e )
    {
      DebugBreak();
    }



    public void StopDebugging()
    {
      if ( InvokeRequired )
      {
        Invoke( new ParameterLessCallback( StopDebugging ) );
      }
      else
      {
        try
        {
          if ( ( m_CurrentActiveTool != null )
          &&   ( !EmulatorSupportsDebugging( m_CurrentActiveTool ) ) )
          {
            if ( RunProcess != null )
            {
              RunProcess.Kill();
              RunProcess = null;
            }
            return;
          }

          if ( ( AppState == Types.StudioState.DEBUGGING_BROKEN )
          ||   ( AppState == Types.StudioState.DEBUGGING_RUN ) )
          {
            // send any command to break into the monitor again
            StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.QUIT );

            if ( StudioCore.Debugging.MarkedDocument != null )
            {
              MarkLine( StudioCore.Debugging.MarkedDocument.DocumentInfo.Project, StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, -1 );
              StudioCore.Debugging.MarkedDocument = null;
            }
            /*
            if ( ( ActiveDocument != StudioCore.Debugging.DebugDisassembly )
            &&   ( ActiveDocumentInfo != null ) )
            {
              MarkLine( ActiveDocumentInfo.Project, ActiveDocumentInfo.FullPath, -1 );
            }
            else
            {
              StudioCore.Debugging.MarkedDocument = null;
              StudioCore.Debugging.MarkedDocumentLine = -1;
            }*/

            if ( StudioCore.Debugging.DebugDisassembly != null )
            {
              StudioCore.Debugging.DebugDisassembly.Close();
              StudioCore.Debugging.DebugDisassembly = null;
            }
            StudioCore.Debugging.CurrentCodePosition = -1;

            StudioCore.Debugging.DebuggedProject = null;
            m_CurrentActiveTool = null;
            StudioCore.Debugging.FirstActionAfterBreak = false;
            mainDebugGo.Enabled = false;
            mainDebugBreak.Enabled = false;
            AppState = Types.StudioState.NORMAL;
          }
        }
        catch ( System.Exception ex )
        {
          Debug.Log( ex.ToString() );
        }
      }
    }



    private void mainDebugStop_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.DEBUG_STOP );
    }



    private void DebugStep()
    {
      if ( ( m_CurrentActiveTool != null )
      &&   ( !EmulatorSupportsDebugging( m_CurrentActiveTool ) ) )
      {
        return;
      }
      if ( ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      ||   ( AppState == Types.StudioState.DEBUGGING_RUN ) )
      {
        m_DebugMemory.InvalidateAllMemory();
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.STEP );
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_VALUES );
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_MEMORY, m_DebugMemory.MemoryStart, m_DebugMemory.MemorySize );

        SetForegroundWindow( this.Handle );

        AppState = Types.StudioState.DEBUGGING_BROKEN;
        if ( AppState == Types.StudioState.DEBUGGING_RUN )
        {
          StudioCore.Debugging.FirstActionAfterBreak = true;
        }
        mainDebugGo.Enabled = false;
        mainDebugBreak.Enabled = true;
      }
    }



    private void DebugStepOver()
    {
      if ( ( m_CurrentActiveTool != null )
      && ( !EmulatorSupportsDebugging( m_CurrentActiveTool ) ) )
      {
        return;
      }
      if ( ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      ||   ( AppState == Types.StudioState.DEBUGGING_RUN ) )
      {
        m_DebugMemory.InvalidateAllMemory();
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.NEXT );
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_VALUES );
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_MEMORY, m_DebugMemory.MemoryStart, m_DebugMemory.MemorySize );

        if ( AppState == Types.StudioState.DEBUGGING_RUN )
        {
          StudioCore.Debugging.FirstActionAfterBreak = true;
        }
        SetForegroundWindow( this.Handle );
        AppState = Types.StudioState.DEBUGGING_BROKEN;
        mainDebugGo.Enabled = true;
        mainDebugBreak.Enabled = false;
      }
    }



    private void mainDebugStepInto_Click( object sender, EventArgs e )
    {
      DebugStep();
    }



    private void mainDebugStepOver_Click( object sender, EventArgs e )
    {
      DebugStepOver();
    }



    private void mainDebugStepOut_Click( object sender, EventArgs e )
    {
      if ( ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      ||   ( AppState == Types.StudioState.DEBUGGING_RUN ) )
      {
        m_DebugMemory.InvalidateAllMemory();
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.RETURN );
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_VALUES );
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_MEMORY, m_DebugMemory.MemoryStart, m_DebugMemory.MemorySize );

        if ( AppState == Types.StudioState.DEBUGGING_RUN )
        {
          StudioCore.Debugging.FirstActionAfterBreak = true;
        }
        SetForegroundWindow( this.Handle );
        AppState = Types.StudioState.DEBUGGING_BROKEN;
        mainDebugGo.Enabled = true;
        mainDebugBreak.Enabled = false;
      }
    }



    private void refreshRegistersToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      ||   ( AppState == Types.StudioState.DEBUGGING_RUN ) )
      {
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_VALUES );
        if ( AppState == Types.StudioState.DEBUGGING_RUN )
        {
          StudioCore.Debugging.FirstActionAfterBreak = true;
        }
        SetForegroundWindow( this.Handle );
        AppState = Types.StudioState.DEBUGGING_BROKEN;
      }
    }



    private GR.Collections.Set<DocumentInfo> FindDocumentsInDependencyChain( DocumentInfo Doc )
    {
      GR.Collections.Set<DocumentInfo>    tempSet = new GR.Collections.Set<DocumentInfo>();

      if ( Doc.Element == null )
      {
        return tempSet;
      }

      tempSet.Add( Doc );
      bool foundElement = true;
      bool addedMainElement = false;
      while ( foundElement )
      {
        foundElement = false;

        retry:
        foreach ( DocumentInfo doc in tempSet )
        {
          if ( ( tempSet.ContainsValue( doc ) )
          &&   ( ( doc != Doc )
          ||     ( addedMainElement ) ) )
          {
            continue;
          }
          if ( doc == Doc )
          {
            addedMainElement = true;
          }
          foreach ( var deducedDependency in doc.DeducedDependency[doc.Project.Settings.CurrentConfig.Name].BuildState.Keys )
          {
            var element = doc.Project.GetElementByFilename( deducedDependency );
            if ( element != null )
            {
              if ( !tempSet.ContainsValue( element.DocumentInfo ) )
              {
                tempSet.Add( element.DocumentInfo );
                foundElement = true;
              }
            }
          }

          if ( doc.Element != null )
          {
            foreach ( var file in doc.Element.ForcedDependency.DependentOnFile )
            {
              var element = doc.Project.GetElementByFilename( file.Filename );
              if ( element != null )
              {
                if ( !tempSet.ContainsValue( element.DocumentInfo ) )
                {
                  tempSet.Add( element.DocumentInfo );
                  foundElement = true;
                }
              }
            }
          }
          if ( foundElement )
          {
            goto retry;
          }
        }
      }
      return tempSet;
    }



    private bool FindAndOpenBestMatchForLocation( int CurrentPos )
    {
      // TODO - check active file first, then active project, then any
      string    documentFile = "";
      int       documentLine = 0;

      DocumentInfo    currentMarkedFile = null;
      if ( StudioCore.Debugging.MarkedDocument != null )
      {
        currentMarkedFile = StudioCore.Debugging.MarkedDocument.DocumentInfo;
      }

      DocumentInfo    activeFile = ActiveDocumentInfo;

      List<DocumentInfo>      foundMatches = new List<DocumentInfo>();


      // find any match
      GR.Collections.Set<DocumentInfo>    dependentDocuments = FindDocumentsInDependencyChain( StudioCore.Debugging.DebugBaseDocumentRun );
      foreach ( DocumentInfo doc in dependentDocuments )
      {
        if ( doc.Type == ProjectElement.ElementType.ASM_SOURCE )
        {
          if ( doc.ASMFileInfo.DocumentAndLineFromAddress( CurrentPos, out documentFile, out documentLine ) )
          {
            if ( ( StudioCore.Debugging.MarkedDocument == null )
            ||   ( !GR.Path.IsPathEqual( StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, documentFile ) )
            ||   ( StudioCore.Debugging.MarkedDocumentLine != documentLine ) )
            {
              foundMatches.Add( doc );
            }
          }
        }
      }

      /*
      Debug.Log( "Found " + foundMatches.Count + " potential matches" );
      Debug.Log( "ActiveFile = " + ( ( activeFile != null ) ? activeFile.FullPath : "null" ) );
      Debug.Log( "currentMarkedFile = " + ( ( currentMarkedFile != null ) ? currentMarkedFile.FullPath : "null" ) );
      foreach ( var docInfo in foundMatches )
      {
        Debug.Log( "-candidate " + docInfo.FullPath );
      }*/

      if ( activeFile != null )
      {
        //Debug.Log( "Try with activefile first" );
        if ( activeFile.ASMFileInfo.DocumentAndLineFromAddress( CurrentPos, out documentFile, out documentLine ) )
        {
          StudioCore.Navigating.OpenDocumentAndGotoLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
          MarkLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
          return true;
        }
      }
      if ( ( currentMarkedFile != null )
      &&   ( currentMarkedFile != activeFile ) )
      {
        //Debug.Log( "Try with activefile first" );
        if ( currentMarkedFile.ASMFileInfo.DocumentAndLineFromAddress( CurrentPos, out documentFile, out documentLine ) )
        {
          StudioCore.Navigating.OpenDocumentAndGotoLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
          MarkLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
          return true;
        }
      }

      // if any left use the first one
      if ( ( foundMatches.Count > 0 )
      &&   ( foundMatches[0].ASMFileInfo.DocumentAndLineFromAddress( CurrentPos, out documentFile, out documentLine ) ) )
      {
        //Debug.Log( "use first of left overs: " + foundMatches[0].FullPath );
        StudioCore.Navigating.OpenDocumentAndGotoLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
        MarkLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
        return true;
      }
      return false;
    }



    public void SetDebuggerValues( string[] RegisterValues )
    {
      if ( InvokeRequired )
      {
        Invoke( new SetDebuggerValuesCallback( SetDebuggerValues ), new object[] { RegisterValues } );
      }
      else
      {
        try
        {
          // ReadRegisters:(C:$0810)   ADDR AC XR YR SP 00 01 NV-BDIZC LIN CYC
          if ( AppState == Types.StudioState.DEBUGGING_RUN )
          {
            StudioCore.Debugging.FirstActionAfterBreak = true;
          }
          SetForegroundWindow( this.Handle );
          AppState = Types.StudioState.DEBUGGING_BROKEN;
          m_DebugRegisters.SetRegisters( RegisterValues[1], RegisterValues[2], RegisterValues[3], RegisterValues[4],
                                         RegisterValues[7], RegisterValues[0].Substring( 2 ), RegisterValues[8], RegisterValues[9], RegisterValues[6] );

          int currentPos = GR.Convert.ToI32( RegisterValues[0].Substring( 2 ), 16 );
          string documentFile = "";
          int documentLine = -1;
          if ( StudioCore.Debugging.DebuggedASMBase.ASMFileInfo.DocumentAndLineFromAddress( currentPos, out documentFile, out documentLine ) )
          {
            if ( ( StudioCore.Debugging.MarkedDocument == null )
            ||   ( !GR.Path.IsPathEqual( StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, documentFile ) )
            ||   ( StudioCore.Debugging.MarkedDocumentLine != documentLine ) )
            {
              StudioCore.Navigating.OpenDocumentAndGotoLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
              MarkLine( StudioCore.Debugging.DebuggedProject, documentFile, documentLine );
            }
          }
          else
          {
            // try to find info in file in dependency chain
            if ( !FindAndOpenBestMatchForLocation( currentPos ) )
            {
              ShowDisassemblyAt( currentPos );
            }
          }
          mainDebugGo.Enabled = true;
          mainDebugBreak.Enabled = false;
        }
        catch ( System.Exception ex )
        {
          Debug.Log( ex.ToString() );
        }
      }
    }



    private void ShowDisassemblyAt( int Address )
    {
      StudioCore.Debugging.CurrentCodePosition = Address;
      if ( StudioCore.Debugging.DebugDisassembly == null )
      {
        StudioCore.Debugging.DebugDisassembly = new Disassembly( StudioCore );
        StudioCore.Debugging.DebugDisassembly.RefreshDisplayOptions();

        StudioCore.Debugging.DebugDisassembly.Name = "Disassembly";
        StudioCore.Debugging.DebugDisassembly.SetDocumentFilename( "Disassembly" );
        StudioCore.Debugging.DebugDisassembly.Show( panelMain );
        //Settings.Tools[StudioCore.Debugging.DebugDisassembly.Text].Visible = true;
      }

      // TODO - put disassembly in there
      StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_MEMORY, Address, 32 );
      StudioCore.Debugging.DebugDisassembly.SetText( "Disassembly will\r\nappear here" );

      StudioCore.Debugging.DebugDisassembly.SetCursorToLine( 1, true );

      if ( ( StudioCore.Debugging.MarkedDocument == null )
      ||   ( !GR.Path.IsPathEqual( StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, "C64Studio-intermediatedisassembly" ) )
      ||   ( StudioCore.Debugging.MarkedDocumentLine != 1 ) )
      {
        if ( StudioCore.Debugging.MarkedDocument != null )
        {
          StudioCore.Debugging.MarkedDocument.SetLineMarked( StudioCore.Debugging.MarkedDocumentLine, false );
        }

        StudioCore.Debugging.MarkedDocument = StudioCore.Debugging.DebugDisassembly;
        StudioCore.Debugging.MarkedDocumentLine = 1;
        StudioCore.Debugging.DebugDisassembly.Select();
        StudioCore.Debugging.DebugDisassembly.SetLineMarked( 1, 1 != -1 );
      }
    }



    private void selfParseToolStripMenuItem_Click( object sender, EventArgs e )
    {
      DocumentInfo    doc = ActiveDocumentInfo;
      if ( doc == null )
      {
        return;
      }
      if ( doc.Type != ProjectElement.ElementType.ASM_SOURCE )
      {
        return;
      }
      EnsureFileIsParsed();

      StudioCore.Compiling.ParserASM.DumpLabels();
    }



    private void showLineinfosToolStripMenuItem_Click( object sender, EventArgs e )
    {
      DocumentInfo    doc = ActiveDocumentInfo;
      if ( doc == null )
      {
        return;
      }
      if ( doc.Type != ProjectElement.ElementType.ASM_SOURCE )
      {
        return;
      }
      EnsureFileIsParsed();
      foreach ( int address in StudioCore.Compiling.ParserASM.ASMFileInfo.AddressToLine.Keys )
      {
        Debug.Log( "Line " + StudioCore.Compiling.ParserASM.ASMFileInfo.AddressToLine[address].ToString() + ": " + address + ", " + StudioCore.Compiling.ParserASM.ASMFileInfo.LineInfo[StudioCore.Compiling.ParserASM.ASMFileInfo.AddressToLine[address]].Line );
      }
      foreach ( Types.ASM.SourceInfo sourceInfo in StudioCore.Compiling.ParserASM.ASMFileInfo.SourceInfo.Values )
      {
        Debug.Log( "Source " + sourceInfo.Filename + " in " + sourceInfo.FilenameParent + " from line " + sourceInfo.GlobalStartLine + " to " + ( sourceInfo.GlobalStartLine + sourceInfo.LineCount - 1 ) );
      }
    }



    private bool IsDocPartOfMainDocument( DocumentInfo Doc )
    {
      if ( Doc.Project == null )
      {
        return false;
      }
      ProjectElement element = Doc.Project.GetElementByFilename( Doc.Project.Settings.MainDocument );
      if ( ( element != null )
      &&   ( element.Document != null ) )
      {
        if ( ( Doc != null )
        &&   ( element.DocumentInfo.Type == ProjectElement.ElementType.ASM_SOURCE ) )
        {
          if ( ( element.DocumentInfo.ASMFileInfo.LineInfo.Count != 0 )
          &&   ( !element.DocumentInfo.ASMFileInfo.IsDocumentPart( Doc.FullPath ) ) )
          {
            return false;
          }
        }
        return true;
      }
      return false;
    }



    public DocumentInfo DetermineDocumentByFileName( string Filename )
    {
      foreach ( var docInfo in DocumentInfos )
      {
        if ( ( !string.IsNullOrEmpty( docInfo.FullPath ) )
        &&   ( GR.Path.IsPathEqual( docInfo.FullPath, Filename ) ) )
        {
          return docInfo;
        }
      }
      return null;
    }



    public DocumentInfo DetermineDocument()
    {
      BaseDocument baseDocToCompile = ActiveContent;
      if ( ( baseDocToCompile != null )
      &&   ( !baseDocToCompile.DocumentInfo.Compilable ) )
      {
        baseDocToCompile = ActiveDocument;
      }
      if ( baseDocToCompile == null )
      {
        return null;
      }
      return baseDocToCompile.DocumentInfo;
    }



    public DocumentInfo DetermineDocumentToCompile()
    {
      BaseDocument baseDocToCompile = ActiveContent;
      if ( ( baseDocToCompile != null )
      &&   ( !baseDocToCompile.DocumentInfo.Compilable ) )
      {
        baseDocToCompile = ActiveDocument;
      }
      if ( baseDocToCompile == null )
      {
        return null;
      }

      DocumentInfo docToCompile = baseDocToCompile.DocumentInfo;


      if ( ( docToCompile.Element != null )
      &&   ( !string.IsNullOrEmpty( docToCompile.Project.Settings.MainDocument ) ) )
      {
        ProjectElement element = docToCompile.Project.GetElementByFilename( docToCompile.Project.Settings.MainDocument );
        if ( element != null )
        //&&   ( element.Document != null ) )
        {
          if ( ( docToCompile != null )
          &&   ( element.DocumentInfo.Type == ProjectElement.ElementType.ASM_SOURCE ) )
          {
            if ( ( element.DocumentInfo.ASMFileInfo.LineInfo.Count != 0 )
            &&   ( docToCompile.Compilable )
            &&   ( !element.DocumentInfo.ASMFileInfo.IsDocumentPart( docToCompile.FullPath ) )
            &&   ( !( element.IsDependentOn( docToCompile.FullPath ) ) ) )
            {
              return docToCompile;
            }
            return element.DocumentInfo;
          }
        }
      }

      // are we included in a different file and we know it?
      if ( ( docToCompile == null )
      ||   ( !docToCompile.Compilable ) )
      {
        if ( baseDocToCompile != null )
        {
          return baseDocToCompile.DocumentInfo;
        }
        return null;
      }
      return docToCompile;
    }



    private DocumentInfo DetermineDocumentToHandle()
    {
      BaseDocument baseDocToCompile = ActiveContent;
      if ( ( baseDocToCompile != null )
      &&   ( !baseDocToCompile.DocumentInfo.Compilable ) )
      {
        baseDocToCompile = ActiveDocument;
      }

      DocumentInfo  docToCompile = baseDocToCompile.DocumentInfo;

      // if there is a main document AND we are part of it's compile chain
      if ( ( docToCompile.Element != null )
      &&   ( !string.IsNullOrEmpty( docToCompile.Project.Settings.MainDocument ) ) )
      {
        ProjectElement element = docToCompile.Project.GetElementByFilename( docToCompile.Project.Settings.MainDocument );
        if ( ( element != null )
        &&   ( element.Document != null ) )
        {
          if ( docToCompile != null )
          {
            if ( ( docToCompile.Compilable )
            &&   ( !element.DocumentInfo.ASMFileInfo.IsDocumentPart( docToCompile.FullPath ) )
            &&   ( !element.IsDependentOn( docToCompile.FullPath ) ) )
            {
              return docToCompile;
            }
          }
          docToCompile = element.DocumentInfo;
        }
      }

      if ( ( docToCompile == null )
      ||   ( !docToCompile.Compilable ) )
      {
        return null;
      }
      return docToCompile;
    }



    public void CallHelp( string Keyword )
    {
      m_ChangingToolWindows = true;
      if ( string.IsNullOrEmpty( m_Help.Text ) )
      {
        m_Help.Text = "Help";
      }
      StudioCore.Settings.Tools[ToolWindowType.HELP].Document.Show( panelMain );
      StudioCore.Settings.Tools[ToolWindowType.HELP].MenuItem.Checked = true;
      StudioCore.Settings.Tools[ToolWindowType.HELP].Visible[m_ActivePerspective] = true;
      m_ChangingToolWindows = false;

      if ( !string.IsNullOrEmpty( Keyword ) )
      {
        if ( StudioCore.Compiling.ParserASM.m_Processor.Opcodes.ContainsKey( Keyword.ToLower() ) )
        {
          m_Help.NavigateTo( "aay64h64/AAY64/B" + Keyword.ToUpper() + ".HTM" );
        }
        else if ( StudioCore.Compiling.ParserASM.ASMFileInfo.AssemblerSettings.Macros.ContainsKey( Keyword.ToUpper() ) )
        {
          m_Help.NavigateTo( "asm_macro.html#" + Keyword.Substring( 1 ).ToLower() );
        }
      }
    }



    public bool ApplyFunction( Types.Function Function )
    {
      switch ( Function )
      {
        case Types.Function.FIND_NEXT_MESSAGE:
          {
            StudioCore.Navigating.OpenSourceOfNextMessage();
          }
          break;
        case C64Studio.Types.Function.OPEN_FILES:
          {
            System.Windows.Forms.OpenFileDialog openDlg = new System.Windows.Forms.OpenFileDialog();
            openDlg.Title = "Open existing item";
            openDlg.Filter = FilterString( Types.Constants.FILEFILTER_ALL_SUPPORTED_FILES + Types.Constants.FILEFILTER_ASM + Types.Constants.FILEFILTER_CHARSET + Types.Constants.FILEFILTER_SPRITE + Types.Constants.FILEFILTER_BASIC + Types.Constants.FILEFILTER_BINARY_FILES + Types.Constants.FILEFILTER_ALL );

            if ( m_CurrentProject != null )
            {
              openDlg.InitialDirectory = m_CurrentProject.Settings.BasePath;
            }
            openDlg.Multiselect = true;
            if ( openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
            {
              return true;
            }

            foreach ( var fileName in openDlg.FileNames )
            {
              OpenFile( fileName );
            }
          }
          break;
        case C64Studio.Types.Function.TOGGLE_BREAKPOINT:
          if ( ( AppState != Types.StudioState.NORMAL )
          &&   ( AppState != C64Studio.Types.StudioState.DEBUGGING_BROKEN ) )
          {
            break;
          }
          if ( ActiveDocument is SourceASMEx )
          {
            SourceASMEx asm = (SourceASMEx)ActiveDocument;

            asm.ToggleBreakpoint( asm.CurrentLineIndex );
          }
          break;
        case C64Studio.Types.Function.HELP:
          {
            string keywordBelow = null;
            if ( ( ActiveContent != null )
            &&   ( ActiveContent is SourceASMEx ) )
            {
              SourceASMEx asm = ActiveContent as SourceASMEx;

              if ( asm.editSource.SelectionLength > 0 )
              {
                keywordBelow = asm.editSource.Selection.Text;
              }
              else
              {
                keywordBelow = asm.FindWordAtCaretPosition();
              }
            }
            CallHelp( keywordBelow );
          }
          return true;
        case C64Studio.Types.Function.DELETE_LINE:
          return false;
        case C64Studio.Types.Function.FIND_NEXT:
          m_FindReplace.FindNext( ActiveDocument );
          break;
        case C64Studio.Types.Function.FIND:
          if ( ActiveDocument is SourceASMEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceASMEx)ActiveDocument ).editSource );
          }
          else if ( ActiveDocument is SourceBasicEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceBasicEx)ActiveDocument ).editSource );
          }
          if ( m_FindReplace.Visible )
          {
            m_FindReplace.comboSearchText.Focus();
          }
          else
          {
            m_FindReplace.Show( panelMain );
          }
          StudioCore.Settings.Tools[ToolWindowType.FIND_REPLACE].Visible[m_ActivePerspective] = true;

          m_FindReplace.tabFindReplace.SelectedIndex = 0;
          m_FindReplace.comboSearchTarget.SelectedIndex = 1;
          m_FindReplace.AcceptButton = m_FindReplace.btnFindNext;
          break;
        case C64Studio.Types.Function.FIND_IN_PROJECT:
          if ( ActiveDocument is SourceASMEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceASMEx)ActiveDocument ).editSource );
          }
          else if ( ActiveDocument is SourceBasicEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceBasicEx)ActiveDocument ).editSource );
          }
          if ( m_FindReplace.Visible )
          {
            m_FindReplace.comboSearchText.Focus();
          }
          else
          {
            m_FindReplace.Show( panelMain );
          }
          StudioCore.Settings.Tools[ToolWindowType.FIND_REPLACE].Visible[m_ActivePerspective] = true;
          m_FindReplace.tabFindReplace.SelectedIndex = 0;
          m_FindReplace.comboSearchTarget.SelectedIndex = 3;
          m_FindReplace.AcceptButton = m_FindReplace.btnFindAll;
          break;
        case C64Studio.Types.Function.FIND_REPLACE:
          if ( ActiveDocument is SourceASMEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceASMEx)ActiveDocument ).editSource );
          }
          else if ( ActiveDocument is SourceBasicEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceBasicEx)ActiveDocument ).editSource );
          }
          if ( m_FindReplace.Visible )
          {
            m_FindReplace.comboReplaceSearchText.Focus();
          }
          else
          {
            m_FindReplace.Show( panelMain );
          }
          StudioCore.Settings.Tools[ToolWindowType.FIND_REPLACE].Visible[m_ActivePerspective] = true;
          m_FindReplace.tabFindReplace.SelectedIndex = 1;
          break;
        case C64Studio.Types.Function.REPLACE_IN_PROJECT:
          if ( ActiveDocument is SourceASMEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceASMEx)ActiveDocument ).editSource );
          }
          else if ( ActiveDocument is SourceBasicEx )
          {
            m_FindReplace.AdjustSettings( ( (SourceBasicEx)ActiveDocument ).editSource );
          }
          if ( m_FindReplace.Visible )
          {
            m_FindReplace.comboReplaceSearchText.Focus();
          }
          else
          {
            m_FindReplace.Show( panelMain );
          }
          StudioCore.Settings.Tools[ToolWindowType.FIND_REPLACE].Visible[m_ActivePerspective] = true;
          m_FindReplace.tabFindReplace.SelectedIndex = 1;
          m_FindReplace.comboReplaceTarget.SelectedIndex = 3;
          break;
        case C64Studio.Types.Function.PRINT:
        case C64Studio.Types.Function.COMMENT_SELECTION:
        case C64Studio.Types.Function.UNCOMMENT_SELECTION:
          {
            var curDoc = ActiveDocumentInfo;
            if ( ( curDoc != null )
            &&   ( curDoc.BaseDoc != null )
            &&   ( curDoc.ContainsCode ) )
            {
              curDoc.BaseDoc.ApplyFunction( Function );
            }
          }
          break;
        case C64Studio.Types.Function.CENTER_ON_CURSOR:
          {
            // save current document
            BaseDocument curDoc = ActiveContent;
            if ( ( curDoc != null )
            &&   ( !curDoc.DocumentInfo.ContainsCode ) )
            {
              curDoc = ActiveDocument;
            }
            if ( ( curDoc != null )
            &&   ( curDoc is SourceASMEx ) )
            {
              SourceASMEx   source = (SourceASMEx)curDoc;

              source.CenterOnCaret();
            }
            if ( ( curDoc != null )
            &&   ( curDoc is SourceBasicEx ) )
            {
              SourceBasicEx source = (SourceBasicEx)curDoc;

              source.CenterOnCaret();
            }
          }
          break;
        case C64Studio.Types.Function.DEBUG_STEP:
          if ( ( StudioCore.Debugging.Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
          &&   ( StudioCore.Debugging.FirstActionAfterBreak ) )
          {
            StudioCore.Debugging.FirstActionAfterBreak = false;
            DebugStep();
          }
          DebugStep();
          break;
        case C64Studio.Types.Function.DEBUG_STEP_OVER:
          if ( ( StudioCore.Debugging.Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
          &&   ( StudioCore.Debugging.FirstActionAfterBreak ) )
          {
            StudioCore.Debugging.FirstActionAfterBreak = false;
            DebugStepOver();
          }
          DebugStepOver();
          break;
        case C64Studio.Types.Function.DEBUG_STOP:
          StopDebugging();
          break;
        case C64Studio.Types.Function.DEBUG_GO:
          DebugGo();
          break;
        case C64Studio.Types.Function.DEBUG_BREAK:
          DebugBreak();
          break;
        case C64Studio.Types.Function.DEBUG_RUN_TO:
          if ( ( AppState != Types.StudioState.NORMAL )
          &&   ( AppState != C64Studio.Types.StudioState.DEBUGGING_BROKEN ) )
          {
            break;
          }
          {
            DocumentInfo docToDebug = DetermineDocumentToHandle();
            DocumentInfo docToHandle = DetermineDocumentToCompile();
            DocumentInfo docActive = DetermineDocument();
            
            if ( docToDebug.Type != ProjectElement.ElementType.ASM_SOURCE )
            {
              break;
            }

            EnsureFileIsParsed( docToHandle );

          Types.ASM.FileInfo debugFileInfo        = StudioCore.Navigating.DetermineASMFileInfo( docToHandle );
          Types.ASM.FileInfo localDebugFileInfo   = StudioCore.Navigating.DetermineLocalASMFileInfo( docToDebug );
          Types.ASM.FileInfo localDebugFileInfo2  = StudioCore.Navigating.DetermineLocalASMFileInfo( docActive );

          int           lineIndex = -1;

          if ( debugFileInfo.FindGlobalLineIndex( docActive.BaseDoc.CurrentLineIndex, docActive.FullPath, out lineIndex ) )
          {
            int targetAddress = debugFileInfo.FindLineAddress( lineIndex );
            if ( targetAddress != -1 )
            {
              if ( ( StudioCore.Debugging.Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
              &&   ( StudioCore.Debugging.FirstActionAfterBreak ) )
              {
                StudioCore.Debugging.FirstActionAfterBreak = false;
                RunToAddress( docToDebug, docToHandle, targetAddress );
              }
              RunToAddress( docToDebug, docToHandle, targetAddress );
            }
            else
            {
              System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
            }
          }
          else if ( localDebugFileInfo.FindGlobalLineIndex( docActive.BaseDoc.CurrentLineIndex, docActive.FullPath, out lineIndex ) )
          {
            // retry at local debug info
            int targetAddress = localDebugFileInfo.FindLineAddress( lineIndex );
            if ( targetAddress != -1 )
            {
              if ( ( StudioCore.Debugging.Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
              &&   ( StudioCore.Debugging.FirstActionAfterBreak ) )
              {
                StudioCore.Debugging.FirstActionAfterBreak = false;
                RunToAddress( docToDebug, docToHandle, targetAddress );
              }
              RunToAddress( docToDebug, docToHandle, targetAddress );
            }
            else
            {
              System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
            }
          }
          else if ( localDebugFileInfo2.FindGlobalLineIndex( docActive.BaseDoc.CurrentLineIndex, docActive.FullPath, out lineIndex ) )
          {
            // retry at local debug info
            int targetAddress = localDebugFileInfo2.FindLineAddress( lineIndex );
            if ( targetAddress != -1 )
            {
              if ( ( StudioCore.Debugging.Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
              &&   ( StudioCore.Debugging.FirstActionAfterBreak ) )
              {
                StudioCore.Debugging.FirstActionAfterBreak = false;
                RunToAddress( docToDebug, docToHandle, targetAddress );
              }
              RunToAddress( docToDebug, docToHandle, targetAddress );
            }
            else
            {
              System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
            }
          }
          else
          {
            System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
          }
            /*
            }
            else if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
            {
              EnsureFileIsParsed( docToDebug );
              Types.ASM.FileInfo localDebugFileInfo = DetermineLocalASMFileInfo( docToDebug );
              Types.ASM.FileInfo localDebugFileInfo2 = DetermineLocalASMFileInfo( docActive );

              int           lineIndex = -1;

              if ( debugFileInfo.FindGlobalLineIndex( docActive.CurrentLineIndex, docActive.FullPath, out lineIndex ) )
              {
                int targetAddress = debugFileInfo.FindLineAddress( lineIndex );
                if ( targetAddress != -1 )
                {
                  if ( ( Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
                  &&   ( m_FirstActionAfterBreak ) )
                  {
                    m_FirstActionAfterBreak = false;
                    RunToAddress( docToDebug, null, targetAddress );
                  }
                  RunToAddress( docToDebug, null, targetAddress );
                }
                else
                {
                  System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
                }
              }
              else if ( localDebugFileInfo.FindGlobalLineIndex( docActive.CurrentLineIndex, docActive.FullPath, out lineIndex ) )
              {
                // retry at local debug info
                int targetAddress = localDebugFileInfo.FindLineAddress( lineIndex );
                if ( targetAddress != -1 )
                {
                  if ( ( Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
                  && ( m_FirstActionAfterBreak ) )
                  {
                    m_FirstActionAfterBreak = false;
                    RunToAddress( docToDebug, docToHandle, targetAddress );
                  }
                  RunToAddress( docToDebug, docToHandle, targetAddress );
                }
                else
                {
                  System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
                }
              }
              else if ( localDebugFileInfo2.FindGlobalLineIndex( docActive.CurrentLineIndex, docActive.FullPath, out lineIndex ) )
              {
                // retry at local debug info
                int targetAddress = localDebugFileInfo2.FindLineAddress( lineIndex );
                if ( targetAddress != -1 )
                {
                  if ( ( Debugger.m_ViceVersion == RemoteDebugger.WinViceVersion.V_2_3 )
                  &&   ( m_FirstActionAfterBreak ) )
                  {
                    m_FirstActionAfterBreak = false;
                    RunToAddress( docToDebug, docToHandle, targetAddress );
                  }
                  RunToAddress( docToDebug, docToHandle, targetAddress );
                }
                else
                {
                  System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
                }
              }
              else
              {
                System.Windows.Forms.MessageBox.Show( "No reachable code was detected in this line (or could not assemble)" );
              }
            }*/
          }
          break;
        case C64Studio.Types.Function.SAVE_ALL:
          SaveSolution();
          if ( m_Solution != null )
          {
            foreach ( Project project in m_Solution.Projects )
            {
              SaveProject( project );
            }
            foreach ( Project project in m_Solution.Projects )
            {
              foreach ( ProjectElement element in project.Elements )
              {
                if ( element.Document != null )
                {
                  element.Document.Save();
                }
              }
            }
            foreach ( Project project in m_Solution.Projects )
            {
              SaveProject( project );
            }
          }
          else
          {
            BaseDocument docToSave = ActiveContent;
            if ( docToSave != null )
            {
              docToSave.Save();
            }
          }
          break;
        case C64Studio.Types.Function.SAVE_DOCUMENT:
          {
            // save current document
            BaseDocument docToSave = ActiveContent;
            if ( ( docToSave != null )
            &&   ( !docToSave.IsSaveable ) )
            {
              docToSave = ActiveDocument;
            }
            if ( ( docToSave == null )
            ||    ( !docToSave.IsSaveable ) )
            {
              break;
            }

            if ( docToSave.DocumentInfo.Project == null )
            {
              docToSave.Save();
              return true;
            }

            if ( ( docToSave.DocumentInfo.Project == null )
            ||   ( docToSave.DocumentInfo.Project.Settings.BasePath == null )
            ||   ( docToSave.DocumentInfo.Element == null ) )
            {
              // no project yet (or no project element)
              if ( !SaveProject( docToSave.DocumentInfo.Project ) )
              {
                return true;
              }
            }
            docToSave.Save();
            if ( !SaveProject( docToSave.DocumentInfo.Project ) )
            {
              return true;
            }
          }
          break;
        case C64Studio.Types.Function.SAVE_DOCUMENT_AS:
          {
            // save current document as
            BaseDocument docToSave = ActiveContent;
            if ( ( docToSave != null )
            &&   ( !docToSave.IsSaveable ) )
            {
              docToSave = ActiveDocument;
            }
            if ( ( docToSave == null )
            ||   ( !docToSave.IsSaveable ) )
            {
              break;
            }

            if ( docToSave.DocumentInfo.Project == null )
            {
              docToSave.SaveAs();
              return true;
            }

            if ( ( docToSave.DocumentInfo.Project == null )
            ||   ( docToSave.DocumentInfo.Project.Settings.BasePath == null )
            ||   ( docToSave.DocumentInfo.Element == null ) )
            {
              // no project yet (or no project element)
              if ( !SaveProject( docToSave.DocumentInfo.Project ) )
              {
                return true;
              }
            }
            docToSave.SaveAs();
            if ( !SaveProject( docToSave.DocumentInfo.Project ) )
            {
              return true;
            }
          }
          break;
        case C64Studio.Types.Function.COMPILE:
          {
            DocumentInfo docToCompile = DetermineDocumentToCompile();
            if ( docToCompile != null )
            {
              Compile( docToCompile );
            }
          }
          break;
        case C64Studio.Types.Function.BUILD:
          {
            DocumentInfo docToCompile = DetermineDocumentToCompile();
            if ( docToCompile != null )
            {
              Build( docToCompile );
            }
          }
          break;
        case C64Studio.Types.Function.REBUILD:
          {
            DocumentInfo docToCompile = DetermineDocumentToCompile();
            if ( docToCompile != null )
            {
              Rebuild( docToCompile );
            }
          }
          break;
        case C64Studio.Types.Function.BUILD_AND_RUN:
          {
            DocumentInfo docToCompile = DetermineDocumentToCompile();
            if ( docToCompile != null )
            {
              BuildAndRun( docToCompile, docToCompile );
            }
          }
          break;
        case C64Studio.Types.Function.BUILD_AND_DEBUG:
          if ( AppState == Types.StudioState.NORMAL )
          {
            DocumentInfo docToCompile = DetermineDocumentToCompile();
            if ( docToCompile != null )
            {
              BuildAndDebug( docToCompile, DetermineDocumentToHandle(), docToCompile );
            }
          }
          else if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
          {
            DebugGo();
          }
          break;
        case C64Studio.Types.Function.GO_TO_DECLARATION:
          {
            DocumentInfo docToDebug = DetermineDocumentToCompile();
            DocumentInfo docToHandle = DetermineDocument();

            if ( docToDebug == null )
            {
              docToDebug = docToHandle;
            }
            if ( docToDebug.Type != ProjectElement.ElementType.ASM_SOURCE )
            {
              break;
            }
            SourceASMEx sourceEx = docToHandle.BaseDoc as SourceASMEx;

            EnsureFileIsParsed( docToDebug );

            if ( sourceEx != null )
            {
              string wordBelow = sourceEx.FindWordAtCaretPosition();
              string zone = sourceEx.FindZoneAtCaretPosition();
              GotoDeclaration( docToHandle, wordBelow, zone );
            }
          }
          break;
        case C64Studio.Types.Function.COPY_LINE_UP:
        case C64Studio.Types.Function.COPY_LINE_DOWN:
        case C64Studio.Types.Function.MOVE_LINE_DOWN:
        case C64Studio.Types.Function.MOVE_LINE_UP:
          // let control handle it
          return false;
      }
      return true;
    }



    public bool HandleCmdKey( ref Message msg, Keys keyData )
    {
      AcceleratorKey usedAccelerator = StudioCore.Settings.DetermineAccelerator( keyData, AppState );
      if ( usedAccelerator != null )
      {
        return ApplyFunction( usedAccelerator.Function );
      }
      return false;
    }



    protected override bool ProcessCmdKey( ref Message msg, Keys keyData )
    {
      AcceleratorKey usedAccelerator = StudioCore.Settings.DetermineAccelerator( keyData, AppState );
      if ( usedAccelerator != null )
      {
        return ApplyFunction( usedAccelerator.Function );
      }
      return base.ProcessCmdKey( ref msg, keyData );
    }



    private void listBreakpointsToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      {
        StudioCore.Debugging.Debugger.SendCommand( "break" );
      }
    }



    public void AddWatchEntry( WatchEntry Watch )
    {
      if ( ( AppState == Types.StudioState.DEBUGGING_RUN )
      ||   ( AppState == Types.StudioState.DEBUGGING_BROKEN )
      ||   ( AppState == Types.StudioState.NORMAL ) )
      {
        m_DebugWatch.AddWatchEntry( Watch );
        StudioCore.Debugging.Debugger.AddWatchEntry( Watch );

        if ( AppState == Types.StudioState.DEBUGGING_BROKEN )
        {
          StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.REFRESH_VALUES );
        }
      }
    }



    public void RemoveWatchEntry( WatchEntry Watch )
    {
      m_DebugWatch.RemoveWatchEntry( Watch );
      StudioCore.Debugging.Debugger.RemoveWatchEntry( Watch );
    }



    public void UpdateWatchInfo( RemoteDebugger.RequestData Request, GR.Memory.ByteBuffer Data )
    {
      if ( InvokeRequired )
      {
        try
        {
          Invoke( new UpdateWatchInfoCallback( UpdateWatchInfo ), new object[] { Request, Data } );
        }
        catch ( System.Exception ex )
        {
          Debug.Log( ex.Message );
        }
      }
      else
      {
        if ( Request.Type == RemoteDebugger.Request.MEM_DUMP )
        {
          if ( Request.Parameter1 == StudioCore.Debugging.CurrentCodePosition )
          {
            if ( StudioCore.Debugging.DebugDisassembly != null )
            {
              // update disassembly
              Parser.Disassembler       disassembler = new C64Studio.Parser.Disassembler( Systems.CPUSystem.Create6510System() );
              string                    disassembly = "";

              disassembler.SetData( Data );

              GR.Collections.Set<int>    jumpedAtAddresses = new GR.Collections.Set<int>();
              jumpedAtAddresses.Add( StudioCore.Debugging.CurrentCodePosition );
              GR.Collections.Map<int,string>    namedLabels = new GR.Collections.Map<int, string>();
              if ( disassembler.Disassemble( StudioCore.Debugging.CurrentCodePosition, jumpedAtAddresses, namedLabels, true, out disassembly ) )
              {
                StudioCore.Debugging.DebugDisassembly.SetText( disassembly );
                StudioCore.Debugging.MarkedDocument.SetLineMarked( StudioCore.Debugging.MarkedDocumentLine, false );

                StudioCore.Debugging.MarkedDocument = StudioCore.Debugging.DebugDisassembly;
                StudioCore.Debugging.MarkedDocumentLine = 1;
                StudioCore.Debugging.DebugDisassembly.Select();
                StudioCore.Debugging.DebugDisassembly.SetLineMarked( 1, 1 != -1 );
              }
              else
              {
                if ( StudioCore.Debugging.MarkedDocument != null )
                {
                  MarkLine( StudioCore.Debugging.MarkedDocument.DocumentInfo.Project, StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, -1 );
                  StudioCore.Debugging.MarkedDocument = null;
                }
                StudioCore.Debugging.DebugDisassembly.SetText( "Disassembly\r\nfailed\r\n" + disassembly );
              }
            }
          }
        }

        if ( Request.Info == "C64Studio.MemDump" )
        {
          m_DebugMemory.UpdateMemory( Request, Data );
        }
        else
        {
          m_DebugWatch.UpdateValue( Request, Data );
        }
      }
    }



    public void EnsureFileIsParsed()
    {
      if ( ActiveDocumentInfo != null )
      {
        EnsureFileIsParsed( ActiveDocumentInfo );
      }
    }



    public bool ParseFile( Parser.ParserBase Parser, DocumentInfo Document, ProjectConfig Configuration )
    {
      C64Studio.Parser.CompileConfig config = new C64Studio.Parser.CompileConfig();
      config.Assembler = Types.AssemblerType.AUTO;
      if ( Document.Element != null )
      {
        config.Assembler = Document.Element.AssemblerType;
      }

      bool result = Parser.ParseFile( Document, Configuration, config );

      if ( Document.Type == ProjectElement.ElementType.ASM_SOURCE )
      {
        C64Studio.Parser.ASMFileParser    asmParser = (C64Studio.Parser.ASMFileParser)Parser;

        Document.ASMFileInfo = asmParser.ASMFileInfo;
      }

      DependencyBuildState  buildState = null;
      if ( Configuration != null )
      {
        buildState = Document.DeducedDependency[Configuration.Name];
        if ( buildState == null )
        {
          buildState = new DependencyBuildState();
          Document.DeducedDependency[Configuration.Name] = buildState;
        }
        buildState.Clear();
      }

      if ( Document.Element != null )
      {
        Document.Element.CompileTarget = Parser.CompileTarget;
        Document.Element.CompileTargetFile = Parser.CompileTargetFile;
      }
      if ( buildState != null )
      {
        foreach ( string dependency in StudioCore.Compiling.ParserASM.ExternallyIncludedFiles )
        {
          DateTime    lastChangeTime = new DateTime();
          try
          {
            lastChangeTime = System.IO.File.GetLastWriteTime( dependency );
          }
          catch
          {
          }
          buildState.BuildState.Add( dependency, lastChangeTime );
        }
      }

      if ( Document.Type == ProjectElement.ElementType.ASM_SOURCE )
      {
        SourceASMEx asm = Document.BaseDoc as SourceASMEx;
        if ( asm != null )
        {
          asm.DoNotFollowZoneSelectors = true;
        }

        if ( ( Document.Project != null )
        &&   ( !string.IsNullOrEmpty( Document.Project.Settings.MainDocument ) )
        &&   ( System.IO.Path.GetFileName( Document.FullPath ) == Document.Project.Settings.MainDocument ) )
        {
          // give all other files the same keywords!
          var knownTokens = StudioCore.Compiling.ParserASM.KnownTokens();
          GR.Collections.MultiMap<string,C64Studio.Types.SymbolInfo> knownTokenInfos = StudioCore.Compiling.ParserASM.KnownTokenInfo();

          // from source info
          GR.Collections.Set<string> filesToUpdate = new GR.Collections.Set<string>();
          foreach ( Types.ASM.SourceInfo sourceInfo in Document.ASMFileInfo.SourceInfo.Values )
          {
            filesToUpdate.Add( sourceInfo.FullPath );
          }

          // from deduced dependencies
          foreach ( var dependencyBuildState in Document.DeducedDependency.Values )
          {
            foreach ( var dependency in dependencyBuildState.BuildState.Keys )
            {
              ProjectElement    element2 = Document.Project.GetElementByFilename( dependency );
              if ( ( element2 != null )
              &&   ( element2.DocumentInfo.Type == ProjectElement.ElementType.ASM_SOURCE ) )
              {
                filesToUpdate.Add( element2.DocumentInfo.FullPath );
              }
            }
          }

          foreach ( string fileToUpdate in filesToUpdate )
          {
            ProjectElement elementToUpdate = Document.Project.GetElementByFilename( fileToUpdate );
            if ( elementToUpdate != null )
            {
              elementToUpdate.DocumentInfo.KnownKeywords = knownTokens;
              elementToUpdate.DocumentInfo.KnownTokens = knownTokenInfos;
              if ( elementToUpdate.Document != null )
              {
                elementToUpdate.Document.OnKnownKeywordsChanged();
                elementToUpdate.Document.OnKnownTokensChanged();
              }
            }
          }
          m_DebugBreakpoints.SetTokens( knownTokenInfos );
        }
        else
        {
          if ( Document != null )
          {
            Document.KnownKeywords = StudioCore.Compiling.ParserASM.KnownTokens();
            Document.KnownTokens = StudioCore.Compiling.ParserASM.KnownTokenInfo();
          }

          if ( !IsDocPartOfMainDocument( Document ) )
          {
            m_DebugBreakpoints.SetTokens( StudioCore.Compiling.ParserASM.KnownTokenInfo() );
          }
        }

        if ( asm != null )
        {
          asm.DoNotFollowZoneSelectors = false;
        }
      }

      if ( InvokeRequired )
      {
        Invoke( new ParseFileCallback( ParseFile ), new object[] { Document, Configuration } );
      }
      else
      {
        StudioCore.Navigating.UpdateFromMessages( Parser.Messages,
                                          ( Parser is Parser.ASMFileParser ) ? ( (Parser.ASMFileParser)Parser ).ASMFileInfo : null,
                                          Document.Project );
        m_CompileResult.UpdateFromMessages( Parser, Document.Project );
      }
      if ( ( result )
      &&   ( Document.BaseDoc != null ) )
      {
        Document.BaseDoc.FileParsed = true;
      }
      return result;
    }



    public void EnsureFileIsParsed( DocumentInfo Document )
    {
      if ( ( ( Document.BaseDoc != null )
      &&     ( !Document.BaseDoc.FileParsed ) )
      ||   ( StudioCore.Compiling.NeedsRebuild( Document ) ) )
      {
        if ( StudioCore.Compiling.NeedsRebuild( Document ) )
        {
          Compile( Document );

          if ( Document.BaseDoc != null )
          {
            Document.BaseDoc.FileParsed = true;
          }
          return;
        }
        if ( Document.BaseDoc != null )
        {
          Document.BaseDoc.FileParsed = true;
        }
        ProjectConfig config = null;
        if ( Document.Element != null )
        {
          config = Document.Project.Settings.Configs[mainToolConfig.SelectedItem.ToString()];
        }
        ParseFile( DetermineParser( Document ), Document, config );
      }
    }



    private void MainForm_FormClosing( object sender, FormClosingEventArgs e )
    {
      if ( e.Cancel )
      {
        return;
      }
      if ( m_CurrentProject == null )
      {
        try
        {
          foreach ( IDockContent dock in panelMain.Contents )
          {
            if ( dock is BaseDocument )
            {
              BaseDocument doc = (BaseDocument)dock;
              if ( doc.Modified )
              {
                DialogResult saveRequestResult = doc.CloseAfterModificationRequest();
                if ( saveRequestResult == DialogResult.Cancel )
                {
                  e.Cancel = true;
                  return;
                }
              }
            }
          }
        }
        catch ( Exception ex )
        {
          Debug.Log( ex.Message );
        }
      }

      // check ALL projects
      if ( m_Solution != null )
      {
        foreach ( Project project in m_Solution.Projects )
        {
          if ( ( project != null )
          && ( project.Modified ) )
          {
            DialogResult result = System.Windows.Forms.MessageBox.Show( "The project " + project.Settings.Name + " has unsaved changes, save now?", "Save Project?", MessageBoxButtons.YesNoCancel );
            if ( result == DialogResult.Cancel )
            {
              e.Cancel = true;
              return;
            }
            e.Cancel = false;
            if ( result == DialogResult.Yes )
            {
              project.Save( project.Settings.Filename );
            }
          }
          else
          {
            e.Cancel = false;
          }
        }
      }
      SaveSettings();
    }



    private void aboutToolStripMenuItem1_Click( object sender, EventArgs e )
    {
      FormAbout   about = new FormAbout();

      about.ShowDialog();
    }



    public void GotoDeclaration( DocumentInfo ASMDoc, string Word, string Zone )
    {
      Types.ASM.FileInfo fileToDebug = StudioCore.Navigating.DetermineASMFileInfo( ASMDoc );

      Types.SymbolInfo tokenInfo = fileToDebug.TokenInfoFromName( Word, Zone );
      if ( tokenInfo == null )
      {
        fileToDebug = ASMDoc.ASMFileInfo;
        tokenInfo = ASMDoc.ASMFileInfo.TokenInfoFromName( Word, Zone );
      }
      if ( tokenInfo != null )
      {
        string documentFile = "";
        int documentLine = -1;
        if ( ( tokenInfo.LineIndex == 0 )
        &&   ( !string.IsNullOrEmpty( tokenInfo.DocumentFilename ) ) )
        {
          // try stored info first
          StudioCore.Navigating.OpenDocumentAndGotoLine( ASMDoc.Project, tokenInfo.DocumentFilename, tokenInfo.LocalLineIndex );
          return;
        }

        if ( fileToDebug.FindTrueLineSource( tokenInfo.LineIndex, out documentFile, out documentLine ) )
        {
          StudioCore.Navigating.OpenDocumentAndGotoLine( ASMDoc.Project, documentFile, documentLine );
        }
      }
      else
      {
        System.Windows.Forms.MessageBox.Show( "Could not determine item source" );
      }
    }



    private void fileNewProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewProjectAndOrSolution();
    }



    private void fileNewASMFileToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.ASM_SOURCE, "ASM File", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void fileNewMapEditorToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.MAP_EDITOR, "Map Project", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void fileNewBasicFileToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.BASIC_SOURCE, "BASIC File", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void fileSetupWizardToolStripMenuItem_Click( object sender, EventArgs e )
    {
      FormWizard    wizard = new FormWizard();

      if ( wizard.ShowDialog() == DialogResult.OK )
      {
        /*
        ToolInfo      toolAssembler = new ToolInfo();

        toolAssembler.Name          = "ACME Assembler";
        toolAssembler.Filename      = wizard.editPathACME.Text;
        toolAssembler.Arguments     = "\"$(Filename)\"";
        toolAssembler.WorkPath      = "\"$(FilePath)\"";
        toolAssembler.Type          = ToolInfo.ToolType.ASSEMBLER;

        Settings.Tools.AddLast( toolAssembler );
         */

        ToolInfo      toolEmulator  = new ToolInfo();

        toolEmulator.Name           = "WinVICE";
        toolEmulator.Filename       = wizard.editPathVice.Text;
        toolEmulator.PRGArguments   = "\"$(RunFilename)\"";
        toolEmulator.CartArguments  = "-cartcrt \"$(RunFilename)\"";
        toolEmulator.WorkPath       = "\"$(RunPath)\"";
        toolEmulator.DebugArguments = "-initbreak 0x$(DebugStartAddressHex) -remotemonitor";
        toolEmulator.TrueDriveOnArguments = "-truedrive +virtualdev";
        toolEmulator.TrueDriveOffArguments = "+truedrive -virtualdev";
        toolEmulator.Type           = ToolInfo.ToolType.EMULATOR;
        StudioCore.Settings.ToolInfos.AddLast( toolEmulator );
      }
    }



    private void MainForm_Load( object sender, EventArgs e )
    {

    }



    private void MainForm_Shown( object sender, EventArgs e )
    {
      if ( StudioCore.Settings.ToolInfos.Count == 0 )
      {
        if ( System.Windows.Forms.MessageBox.Show( "There are currently no tools setup. Do you want to do this now?", "Setup Tools", MessageBoxButtons.YesNo ) == DialogResult.Yes )
        {
          fileSetupWizardToolStripMenuItem_Click( this, null );
        }
      }
    }



    public void MainForm_DragDrop( object sender, DragEventArgs e )
    {
      if ( !e.Data.GetDataPresent( DataFormats.FileDrop ) )
      {
        return;
      }
      string[] fileList = (string[])e.Data.GetData( DataFormats.FileDrop );

      foreach ( string file in fileList )
      {
        OpenFile( file );
      }
    }



    public void CloseSolution()
    {
      if ( m_Solution != null )
      {
        CloseAllProjects();
        m_Solution.Projects.Clear();
        m_Solution = null;
        StudioCore.Settings.LastSolutionWasEmpty = true;

        // clear entries
        m_DebugWatch.ClearAllWatchEntries();
        StudioCore.Debugging.Debugger.ClearAllWatchEntries();
        StudioCore.Debugging.Debugger.ClearAllBreakpoints();

        RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.SOLUTION_CLOSED ) );
      }
    }



    public bool OpenSolution( string Filename )
    {
      CloseSolution();

      //AddTask( new C64Studio.Tasks.TaskOpenSolution( Filename ) );
      m_Solution = new Solution( this );

      GR.Memory.ByteBuffer    solutionData = GR.IO.File.ReadAllBytes( Filename );
      if ( solutionData == null )
      {
        m_Solution = null;
        return false;
      }
      if ( !m_Solution.FromBuffer( solutionData, Filename ) )
      {
        StudioCore.Settings.RemoveFromMRU( Filename, this );
        CloseSolution();
        m_Solution = null;
        return false;
      }
      StudioCore.Settings.UpdateInMRU( Filename, this );
      StudioCore.Settings.LastSolutionWasEmpty = false;

      m_Solution.Modified = false;

      RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.SOLUTION_OPENED ) );
      return true;
    }



    public void SaveSolution()
    {
      if ( m_Solution == null )
      {
        return;
      }
      if ( string.IsNullOrEmpty( m_Solution.Filename ) )
      {
        System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog();

        saveDlg.Title = "Save Solution as";
        saveDlg.Filter = FilterString( Types.Constants.FILEFILTER_SOLUTION + Types.Constants.FILEFILTER_ALL );
        if ( saveDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
        {
          return;
        }
        m_Solution.Filename = saveDlg.FileName;
      }
      GR.IO.File.WriteAllBytes( m_Solution.Filename, m_Solution.ToBuffer( m_Solution.Filename ) );
      m_Solution.Modified = false;
      StudioCore.Settings.UpdateInMRU( m_Solution.Filename, this );
    }



    public BaseDocument OpenFile( string Filename )
    {
      BaseDocument document = null;

      string    extension = System.IO.Path.GetExtension( Filename ).ToUpper();
      if ( extension == ".C64" )
      {
        OpenProject( Filename );
        return null;
      }
      else if ( extension == ".S64" )
      {
        OpenSolution( Filename );
        return null;
      }
      else if ( ( extension == ".D64" )
      ||        ( extension == ".D81" )
      ||        ( extension == ".T64" )
      ||        ( extension == ".PRG" ) )
      {
        document = new FileManager( StudioCore, Filename );
        document.ShowHint = DockState.Float;
      }
      else if ( ( extension == ".SPRITEPROJECT" )
      ||        ( extension == ".SPR" ) )
      {
        document = new SpriteEditor( StudioCore );
        document.ShowHint = DockState.Document;
      }
      else if ( ( extension == ".CHARSETPROJECT" )
      ||        ( extension == ".CHR" ) )
      {
        document = new CharsetEditor( StudioCore );
        document.ShowHint = DockState.Document;
      }
      else if ( extension == ".CHARSCREEN" )
      {
        document = new CharsetScreenEditor( StudioCore );
        document.ShowHint = DockState.Document;
      }
      else if ( extension == ".GRAPHICSCREEN" )
      {
        document = new GraphicScreenEditor( StudioCore );
        document.ShowHint = DockState.Document;
      }
      else if ( extension == ".BAS" )
      {
        document = new SourceBasicEx( StudioCore );
        document.ShowHint = DockState.Document;
      }
      else if ( extension == ".MAPPROJECT" )
      {
        document = new MapEditor( StudioCore );
        document.ShowHint = DockState.Document;
      }
      else
      {
        document = new SourceASMEx( StudioCore );
        document.ShowHint = DockState.Document;
      }

      document.Core = StudioCore;
      document.SetDocumentFilename( Filename );
      document.Text = System.IO.Path.GetFileName( Filename );
      document.Load();
      document.Show( panelMain );
      document.Icon = IconFromType( document.DocumentInfo );
      document.DocumentInfo.UndoManager.MainForm = this;

      RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.DOCUMENT_INFO_CREATED, document.DocumentInfo ) );

      return document;
    }



    public Icon IconFromType( DocumentInfo DocInfo )
    {
      if ( DocInfo.Element == null )
      {
        return System.Drawing.SystemIcons.Asterisk;
      }

      switch ( DocInfo.Type )
      {
        case ProjectElement.ElementType.ASM_SOURCE:
          return C64Studio.Properties.Resources.source;
        case ProjectElement.ElementType.BASIC_SOURCE:
          return C64Studio.Properties.Resources.source_basic;
        case ProjectElement.ElementType.CHARACTER_SCREEN:
          return C64Studio.Properties.Resources.charsetscreen;
        case ProjectElement.ElementType.CHARACTER_SET:
          return C64Studio.Properties.Resources.charset;
        case ProjectElement.ElementType.FOLDER:
          return C64Studio.Properties.Resources.folder;
        case ProjectElement.ElementType.GRAPHIC_SCREEN:
          return C64Studio.Properties.Resources.graphicscreen;
        case ProjectElement.ElementType.MAP_EDITOR:
          return C64Studio.Properties.Resources.mapeditor;
        case ProjectElement.ElementType.PROJECT:
          return C64Studio.Properties.Resources.project;
        case ProjectElement.ElementType.SOLUTION:
          return C64Studio.Properties.Resources.solution;
        case ProjectElement.ElementType.SPRITE_SET:
          return C64Studio.Properties.Resources.spriteset;
        case ProjectElement.ElementType.BINARY_FILE:
          return C64Studio.Properties.Resources.binary;
      }
      return System.Drawing.SystemIcons.Asterisk;
    }



    public BaseDocument CreateNewDocument( ProjectElement.ElementType Type, Project Project )
    {
      BaseDocument    newDoc = null;

      switch ( Type )
      {
        case ProjectElement.ElementType.ASM_SOURCE:
          //newDoc = new SourceASM( this );
          newDoc = new SourceASMEx( StudioCore );
          break;
        case ProjectElement.ElementType.BASIC_SOURCE:
          newDoc = new SourceBasicEx( StudioCore );
          break;
        case ProjectElement.ElementType.CHARACTER_SCREEN:
          newDoc = new CharsetScreenEditor( StudioCore );
          break;
        case ProjectElement.ElementType.CHARACTER_SET:
          newDoc = new CharsetEditor( StudioCore );
          break;
        case ProjectElement.ElementType.GRAPHIC_SCREEN:
          newDoc = new GraphicScreenEditor( StudioCore );
          break;
        case ProjectElement.ElementType.MAP_EDITOR:
          newDoc = new MapEditor( StudioCore );
          break;
        case ProjectElement.ElementType.SPRITE_SET:
          newDoc = new SpriteEditor( StudioCore );
          break;
        case ProjectElement.ElementType.DISASSEMBLER:
          newDoc = new Disassembler( StudioCore );
          break;
        case ProjectElement.ElementType.BINARY_FILE:
          newDoc = new BinaryDisplay( StudioCore, null, true, false );
          break;
      }
      if ( newDoc != null )
      {
        newDoc.DocumentInfo.Project = Project;
        newDoc.DocumentInfo.Type = Type;
        newDoc.DocumentInfo.UndoManager.MainForm = this;
        newDoc.ShowHint = DockState.Document;
        newDoc.Core = StudioCore;
        newDoc.Text = "*";
        newDoc.Show( panelMain );
        newDoc.DocumentInfo.Project = Project;
        newDoc.Icon = IconFromType( newDoc.DocumentInfo );
      }
      return newDoc;
    }



    bool ChooseFilename( ProjectElement.ElementType Type, string DefaultName, Project ParentProject, out string NewName )
    {
      System.Windows.Forms.OpenFileDialog openDlg = new System.Windows.Forms.OpenFileDialog();
      openDlg.Title = "Specify new " + DefaultName;
      NewName = "";

      string    filterSource = "";
      switch ( Type )
      {
        case ProjectElement.ElementType.ASM_SOURCE:
          filterSource += Types.Constants.FILEFILTER_ASM;
          break;
        case ProjectElement.ElementType.BASIC_SOURCE:
          filterSource += Types.Constants.FILEFILTER_BASIC;
          break;
        case ProjectElement.ElementType.CHARACTER_SCREEN:
          filterSource += Types.Constants.FILEFILTER_CHARSET_SCREEN;
          break;
        case ProjectElement.ElementType.CHARACTER_SET:
          filterSource += Types.Constants.FILEFILTER_CHARSET_PROJECT;
          break;
        case ProjectElement.ElementType.GRAPHIC_SCREEN:
          filterSource += Types.Constants.FILEFILTER_GRAPHIC_SCREEN;
          break;
        case ProjectElement.ElementType.SPRITE_SET:
          filterSource += Types.Constants.FILEFILTER_SPRITE_PROJECT;
          break;
        case ProjectElement.ElementType.MAP_EDITOR:
          filterSource += Types.Constants.FILEFILTER_MAP;
          break;
      }
      openDlg.Filter = FilterString( filterSource );
      if ( ParentProject != null )
      {
        openDlg.InitialDirectory = ParentProject.Settings.BasePath;
      }
      openDlg.CheckFileExists = false;
      openDlg.CheckPathExists = true;
      if ( openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return false;
      }
      NewName = openDlg.FileName;
      return true;
    }



    public void AddNewElement( ProjectElement.ElementType Type, string Description, Project ParentProject, TreeNode ParentNode )
    {
      string newFilename;
      if ( !ChooseFilename( Type, Description, ParentProject, out newFilename ) )
      {
        return;
      }

      if ( ParentProject != null )
      {
        foreach ( ProjectElement projElement in ParentProject.Elements )
        {
          if ( GR.Path.IsPathEqual( newFilename, ParentProject.FullPath( projElement.Filename ) ) )
          {
            System.Windows.Forms.MessageBox.Show( "File " + newFilename + " is already part of this project", "File already added" );
            return;
          }
        }
      }
      if ( System.IO.File.Exists( newFilename ) )
      {
        var result = System.Windows.Forms.MessageBox.Show( "There is already an existing file at " + newFilename + ".\r\nDo you want to overwrite it?", "Overwrite existing file?", MessageBoxButtons.YesNo );
        if ( result == DialogResult.No )
        {
          return;
        }
      }

      if ( ParentProject != null )
      {
        string localizedFilename = GR.Path.RelativePathTo( System.IO.Path.GetFullPath( ParentProject.Settings.BasePath ), true, newFilename, false );

        ProjectElement el = CreateNewElement( Type, Description, ParentProject, ParentNode );
        el.Filename = localizedFilename;
        el.Node.Text = System.IO.Path.GetFileName( localizedFilename );
        el.Document.SetDocumentFilename( localizedFilename );
        el.Document.Save();
      }
    }



    public void MainForm_DragEnter( object sender, DragEventArgs e )
    {
      e.Effect = DragDropEffects.All;
      /*
      if ( !e.Data.GetDataPresent( DataFormats.FileDrop ) )
      {
        e.Effect = DragDropEffects.None;
      }
      else
      {
        e.Effect = DragDropEffects.Link;
      }*/
    }



    public void AddTask( Tasks.Task Task )
    {
      m_Tasks.Add( Task );
      Task.Main = this;
      Task.TaskFinished += new C64Studio.Tasks.Task.delTaskFinished( Task_TaskFinished );

      if ( m_CurrentTask == null )
      {
        m_CurrentTask = m_Tasks[0];
        m_Tasks.RemoveAt( 0 );

        System.Threading.Thread workerThread = new System.Threading.Thread( new System.Threading.ThreadStart( m_CurrentTask.RunTask ) );

        StudioCore.SetStatus( m_CurrentTask.Description, true, 0 );
        
        workerThread.Start();
      }
    }



    void Task_TaskFinished( C64Studio.Tasks.Task FinishedTask )
    {
      m_CurrentTask = null;

      switch ( FinishedTask.Type )
      {
        case C64Studio.Tasks.Task.TaskType.PARSE_FILE:
          break;
        case C64Studio.Tasks.Task.TaskType.OPEN_SOLUTION:
          {
            var taskOS = (C64Studio.Tasks.TaskOpenSolution)FinishedTask;

            if ( !FinishedTask.TaskSuccessful )
            {
              StudioCore.Settings.RemoveFromMRU( taskOS.SolutionFilename, this );
              CloseSolution();
            }
            else
            {
              StudioCore.Settings.UpdateInMRU( taskOS.SolutionFilename, this );
              m_Solution = taskOS.Solution;
              m_Solution.Modified = false;
              RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.SOLUTION_OPENED ) );
            }
          }
          break;
      }

      StudioCore.SetStatus( "Ready", false, 0 );

      if ( m_Tasks.Count > 0 )
      {
        m_CurrentTask = m_Tasks[0];
        m_Tasks.RemoveAt( 0 );

        System.Threading.Thread workerThread = new System.Threading.Thread( new System.Threading.ThreadStart( m_CurrentTask.RunTask ) );

        StudioCore.SetStatus( m_CurrentTask.Description, true, 0 );

        workerThread.Start();
      }
    }



    private void mainToolConfig_SelectedIndexChanged( object sender, EventArgs e )
    {
      m_CurrentProject.Settings.CurrentConfig = m_CurrentProject.Settings.Configs[mainToolConfig.SelectedItem.ToString()];

      ProjectConfigChanged();
    }



    private void fileNewSpriteFileToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.SPRITE_SET, "Sprite Set", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void fileNewCharacterFileToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.CHARACTER_SET, "Character Set", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void mainToolNewSpriteFile_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.SPRITE_SET, "Sprite Set", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void mainToolNewCharsetFile_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.CHARACTER_SET, "Character Set", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void mainToolUndo_Click( object sender, EventArgs e )
    {
      if ( ActiveDocument != null )
      {
        ActiveDocument.Undo();
        UpdateUndoSettings();
      }
    }



    private void mainToolRedo_Click( object sender, EventArgs e )
    {
      if ( ActiveDocument != null )
      {
        ActiveDocument.Redo();
        UpdateUndoSettings();
      }
    }



    public void UpdateUndoSettings()
    {
      if ( ActiveDocument == null )
      {
        mainToolUndo.Enabled = false;
        mainToolRedo.Enabled = false;
        undoToolStripMenuItem.Enabled = false;
        redoToolStripMenuItem.Enabled = false;

        copyToolStripMenuItem.Enabled   = false;
        cutToolStripMenuItem.Enabled    = false;
        pasteToolStripMenuItem.Enabled  = false;
        deleteToolStripMenuItem.Enabled = false;
        return;
      }
      if ( InvokeRequired )
      {
        Invoke( new ParameterLessCallback( UpdateUndoSettings ) );
      }
      else
      {
        mainToolUndo.Enabled = ActiveDocument.UndoPossible;
        mainToolRedo.Enabled = ActiveDocument.RedoPossible;
        undoToolStripMenuItem.Enabled = ActiveDocument.UndoPossible;
        redoToolStripMenuItem.Enabled = ActiveDocument.RedoPossible;

        copyToolStripMenuItem.Enabled = ActiveDocument.CopyPossible;
        cutToolStripMenuItem.Enabled = ActiveDocument.CutPossible;
        pasteToolStripMenuItem.Enabled = ActiveDocument.PastePossible;
        deleteToolStripMenuItem.Enabled = ActiveDocument.DeletePossible;


        bool modifications = false;
        foreach ( BaseDocument doc in panelMain.Contents )
        {
          if ( doc.Modified )
          {
            modifications = true;
            break;
          }
        }
        saveToolStripMenuItem.Enabled = ActiveDocument.Modified;
        saveAsToolStripMenuItem.Enabled = true;
        saveAllToolStripMenuItem.Enabled = modifications;
        mainToolSave.Enabled = ActiveDocument.Modified;
        mainToolSaveAll.Enabled = modifications;
      }
    }



    private void undoToolStripMenuItem_Click( object sender, EventArgs e )
    {
      mainToolUndo_Click( null, null );
    }



    private void redoToolStripMenuItem_Click( object sender, EventArgs e )
    {
      mainToolRedo_Click( null, null );
    }



    private void mainToolCompile_Click_1( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.COMPILE );
    }



    public void OnDocumentExternallyChanged( BaseDocument Doc )
    {
      if ( InvokeRequired )
      {
        if ( m_ExternallyChangedDocuments.ContainsValue( Doc ) )
        {
          return;
        }
        m_ExternallyChangedDocuments.Add( Doc );
        BeginInvoke( new DocCallback( OnDocumentExternallyChanged ), new object[] { Doc } );
      }
      else
      {
        if ( System.Windows.Forms.MessageBox.Show( this, "The file " + Doc.DocumentInfo.FullPath + " has changed externally. Do you want to reload the file?", "Reload externally changed file?", MessageBoxButtons.YesNo ) == DialogResult.Yes )
        {
          int cursorLine = Doc.CursorLine;
          Doc.Load();
          Doc.SetModified();
          Doc.SetCursorToLine( cursorLine, true );
        }
        m_ExternallyChangedDocuments.Remove( Doc );
      }
    }



    private void projectOpenTapeDiskFileMenuItem_Click( object sender, EventArgs e )
    {
      System.Windows.Forms.OpenFileDialog openDlg = new System.Windows.Forms.OpenFileDialog();
      openDlg.Title = "Open Tape/Disk File";
      openDlg.Filter = FilterString( Types.Constants.FILEFILTER_MEDIA_FILES + Types.Constants.FILEFILTER_TAPE + Types.Constants.FILEFILTER_DISK + Types.Constants.FILEFILTER_ALL );
      if ( openDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK )
      {
        return;
      }
      OpenFile( openDlg.FileName );
    }



    private void newTapeImageToolStripMenuItem_Click( object sender, EventArgs e )
    {
      FileManager doc = new FileManager( StudioCore, "" );
      doc.ShowHint = DockState.Float;
      doc.CreateEmptyTapeImage();
      doc.Core = StudioCore;
      doc.Show( panelMain );
    }



    private void newDiskImageToolStripMenuItem_Click( object sender, EventArgs e )
    {
      FileManager doc = new FileManager( StudioCore, "" );
      doc.ShowHint = DockState.Float;
      doc.CreateEmptyDiskImage();
      doc.Core = StudioCore;
      doc.Show( panelMain );
    }



    private void emptyTapeT64ToolStripMenuItem_Click( object sender, EventArgs e )
    {
      FileManager doc = new FileManager( StudioCore, "" );
      doc.ShowHint = DockState.Float;
      doc.CreateEmptyTapeImage();
      doc.Core = StudioCore;
      doc.Show( panelMain );
    }



    private void emptyDiskD64ToolStripMenuItem_Click( object sender, EventArgs e )
    {
      FileManager doc = new FileManager( StudioCore, "" );
      doc.ShowHint = DockState.Float;
      doc.CreateEmptyDiskImage();
      doc.Core = StudioCore;
      doc.Show( panelMain );
    }



    private void mainToolNewBasicFile_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.BASIC_SOURCE, "BASIC File", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void fileNewScreenEditorToolStripMenuItem_Click( object sender, EventArgs e )
    {
      BaseDocument document = new GraphicScreenEditor( StudioCore );
      document.ShowHint = DockState.Document;
      document.Core = StudioCore;
      document.Text = "New Graphic Screen";
      document.Load();
      document.Show( panelMain );
    }



    private void propertiesToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( m_CurrentProject == null )
      {
        return;
      }
      ProjectProperties dlgProps = new ProjectProperties( m_CurrentProject, m_CurrentProject.Settings, StudioCore );
      dlgProps.ShowDialog();

      if ( dlgProps.Modified )
      {
        m_CurrentProject.SetModified();
      }
    }



    private void helpToolStripMenuItem1_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.HELP );
    }



    private void disassembleToolsToolStripMenuItem_Click( object sender, EventArgs e )
    {
      /*
      Dialogs.DlgDisassembler   dlgDis = new C64Studio.Dialogs.DlgDisassembler( this );

      dlgDis.ShowDialog();
       */

      CreateNewDocument( ProjectElement.ElementType.DISASSEMBLER, m_CurrentProject );
    }



    private void fileNewCharacterScreenEditorToolStripMenuItem_Click( object sender, EventArgs e )
    {
      BaseDocument document = new CharsetScreenEditor( StudioCore );
      document.ShowHint = DockState.Document;
      document.Core = StudioCore;
      document.Text = "New Character Screen";
      document.Load();
      document.Show( panelMain );
    }



    private void fileOpenToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.OPEN_FILES );
    }



    private void searchToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.FIND );
    }



    private void findReplaceToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.FIND_REPLACE );
    }



    private void mainToolFind_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.FIND );
    }



    private void mainToolFindReplace_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.FIND_REPLACE );
    }



    private void mainToolPrint_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.PRINT );
    }



    private void mainToolSaveAll_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.SAVE_ALL );
    }



    private void saveToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.SAVE_DOCUMENT );
    }



    private void saveAsToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.SAVE_DOCUMENT_AS );
    }



    private void saveAllToolStripMenuItem_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.SAVE_ALL );
    }



    private void menuWindowToolbarMain_Click( object sender, EventArgs e )
    {
      StudioCore.Settings.ToolbarActiveMain = menuWindowToolbarMain.Checked;
      mainTools.Visible = StudioCore.Settings.ToolbarActiveMain;
    }



    private void menuWindowToolbarDebugger_Click( object sender, EventArgs e )
    {
      StudioCore.Settings.ToolbarActiveDebugger = menuWindowToolbarDebugger.Checked;
      debugTools.Visible = StudioCore.Settings.ToolbarActiveDebugger;
    }



    public bool OnVirtualBreakpointReached( Types.Breakpoint Breakpoint )
    {
      Debug.Log( "OnVirtualBreakpointReached" );
      bool    addedRequest = false;
      RemoteDebugger.RequestData prevRequest = null;
      foreach ( var virtualBP in Breakpoint.Virtual )
      {
        if ( !virtualBP.IsVirtual )
        {
          continue;
        }
        int   errorPos = -1;

        var tokenInfos = StudioCore.Compiling.ParserASM.ParseTokenInfo( virtualBP.Expression, 0, virtualBP.Expression.Length, out errorPos );
        if ( errorPos != -1 )
        {
          StudioCore.AddToOutput( "Failed to ParseTokenInfo" + System.Environment.NewLine );
          continue;
        }
        int   result = -1;
        if ( !StudioCore.Compiling.ParserASM.EvaluateTokens( -1, tokenInfos, out result ) )
        {
          StudioCore.AddToOutput( "Failed to evaluate " + virtualBP.Expression + System.Environment.NewLine );
          continue;
        }
        if ( ( result < 0 )
        ||   ( result >= 65536 ) )
        {
          StudioCore.AddToOutput( "Evaluated address out of range " + result + System.Environment.NewLine );
          continue;
        }

        if ( prevRequest != null )
        {
          prevRequest.LastInGroup = false;
        }

        int     traceSize = 1;
        RemoteDebugger.RequestData requData    = new RemoteDebugger.RequestData( RemoteDebugger.Request.TRACE_MEM_DUMP );
        requData.Parameter1 = result;
        requData.Parameter2 = result + traceSize - 1;
        requData.MemDumpOffsetX = false; //watchEntry.IndexedX;
        requData.MemDumpOffsetY = false; //watchEntry.IndexedY;
        requData.Info = virtualBP.Expression;
        requData.Breakpoint = Breakpoint;
        StudioCore.Debugging.Debugger.QueueRequest( requData );

        if ( requData.Parameter2 >= 0x10000 )
        {
          requData.Parameter2 = 0xffff;
        }

        prevRequest = requData;

        addedRequest = true;
      }
      if ( !addedRequest )
      {
        // and auto-go on with debugging
        StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.EXIT );
        return false;
      }
      return true;
    }



    public bool OnInitialBreakpointReached( int Address, int BreakpointIndex )
    {
      if ( StudioCore.Debugging.BreakpointsToAddAfterStartup.Count == 0 )
      {
        return false;
      }
      // now add all later breakpoints
      foreach ( Types.Breakpoint bp in StudioCore.Debugging.BreakpointsToAddAfterStartup )
      {
        if ( ( bp.TriggerOnLoad )
        ||   ( bp.TriggerOnStore ) )
        {
          if ( bp.TriggerOnExec )
          {
            // this was already added, remove
            RemoteDebugger.RequestData requData = new RemoteDebugger.RequestData( RemoteDebugger.Request.DELETE_BREAKPOINT, bp.RemoteIndex );
            requData.Breakpoint = bp;
            StudioCore.Debugging.Debugger.QueueRequest( requData );
            bp.RemoteIndex = -1;
          }
          RemoteDebugger.RequestData delData = new RemoteDebugger.RequestData( RemoteDebugger.Request.ADD_BREAKPOINT, bp.Address );
          delData.Breakpoint = bp;
          StudioCore.Debugging.Debugger.QueueRequest( delData );
        }
      }
      // only auto-go on if the initial break point was not the fake first breakpoint
      if ( Address != StudioCore.Debugging.LateBreakpointOverrideDebugStart )
      {
        // need to add new intermediate break point
        Types.Breakpoint bpTemp = new C64Studio.Types.Breakpoint();

        bpTemp.Address = StudioCore.Debugging.LateBreakpointOverrideDebugStart;
        bpTemp.TriggerOnExec = true;
        bpTemp.Temporary = true;

        StudioCore.Debugging.Debugger.AddBreakpoint( bpTemp );
        /*
        RemoteDebugger.RequestData addNewBP = new RemoteDebugger.RequestData( RemoteDebugger.Request.ADD_BREAKPOINT, m_LateBreakpointOverrideDebugStart );
        addNewBP.Breakpoint = bpTemp;
        Debugger.QueueRequest( addNewBP );*/
      }
      // and auto-go on with debugging
      StudioCore.Debugging.Debugger.QueueRequest( RemoteDebugger.Request.EXIT );

      if ( StudioCore.Debugging.MarkedDocument != null )
      {
        MarkLine( StudioCore.Debugging.MarkedDocument.DocumentInfo.Project, StudioCore.Debugging.MarkedDocument.DocumentInfo.FullPath, -1 );
        StudioCore.Debugging.MarkedDocument = null;
      }

      if ( ( RunProcess != null )
      &&   ( RunProcess.MainWindowHandle != IntPtr.Zero ) )
      {
        SetForegroundWindow( RunProcess.MainWindowHandle );
      }

      StudioCore.Debugging.FirstActionAfterBreak = false;
      mainDebugGo.Enabled = false;
      mainDebugBreak.Enabled = true;
      return true;
    }



    private void fileNewBinaryEditorToolStripMenuItem_Click( object sender, EventArgs e )
    {
      GR.Memory.ByteBuffer emptyData = new GR.Memory.ByteBuffer( 2 );
      BaseDocument document = new BinaryDisplay( StudioCore, emptyData, true, true );
      document.ShowHint = DockState.Document;
      document.Core = StudioCore;
      document.Text = "New Binary Data";
      document.Load();
      document.Show( panelMain );
    }



    public string GetElementText( ProjectElement Element )
    {
      string elementPath = "";
      if ( System.IO.Path.IsPathRooted( Element.Filename ) )
      {
        elementPath = Element.Filename;
      }
      else
      {
        elementPath = GR.Path.Normalize( GR.Path.Append( Element.DocumentInfo.Project.Settings.BasePath, Element.Filename ), false );
      }

      if ( Element.Document != null )
      {
        if ( Element.Document is SourceASMEx )
        {
          DateTime    lastModificationTimeStamp = ( (SourceASMEx)Element.Document ).LastChange;

          if ( ( GR.Path.IsPathEqual( StudioCore.Searching.PreviousSearchedFile, elementPath ) )
          &&   ( lastModificationTimeStamp <= StudioCore.Searching.PreviousSearchedFileTimeStamp ) ) 
          {
            return StudioCore.Searching.PreviousSearchedFileContent;
          }
          StudioCore.Searching.PreviousSearchedFile = elementPath;
          StudioCore.Searching.PreviousSearchedFileTimeStamp = lastModificationTimeStamp;
          StudioCore.Searching.PreviousSearchedFileContent = ( (SourceASMEx)Element.Document ).editSource.Text;
          return StudioCore.Searching.PreviousSearchedFileContent;
        }
        else if ( Element.Document is SourceBasicEx )
        {
          StudioCore.Searching.PreviousSearchedFile = elementPath;
          return ( (SourceBasicEx)Element.Document ).editSource.Text;
        }
        return "";
      }

      // can we use cached text?
      bool    cacheIsUpToDate = false;

      DateTime    lastAccessTimeStamp;

      try
      {
        lastAccessTimeStamp = System.IO.File.GetLastWriteTime( elementPath );

        cacheIsUpToDate = ( lastAccessTimeStamp <= StudioCore.Searching.PreviousSearchedFileTimeStamp );

        StudioCore.Searching.PreviousSearchedFileTimeStamp = lastAccessTimeStamp;
      }
      catch ( Exception )
      {
      }

      if ( ( GR.Path.IsPathEqual( StudioCore.Searching.PreviousSearchedFile, elementPath ) )
      &&   ( cacheIsUpToDate )
      &&   ( StudioCore.Searching.PreviousSearchedFileContent != null ) )
      {
        return StudioCore.Searching.PreviousSearchedFileContent;
      }

      StudioCore.Searching.PreviousSearchedFileContent = GR.IO.File.ReadAllText( elementPath );
      StudioCore.Searching.PreviousSearchedFile = elementPath;
      return StudioCore.Searching.PreviousSearchedFileContent;
    }



    private void dumpLabelsToolStripMenuItem_Click( object sender, EventArgs e )
    {
      DumpPanes( panelMain, "" );
    }



    private void DumpElementHierarchy( TreeNode Node, string Indent )
    {
      Project project = m_SolutionExplorer.ProjectFromNode( Node );
      ProjectElement element = m_SolutionExplorer.ElementFromNode( Node );
      if ( ( element == null )
      &&   ( Node.Level > 0 ) )
      {
        Debug.Log( Indent + Node.Text );
      }
      else
      {
        if ( element == null )
        {
          Debug.Log( Indent + Node.Text );
        }
        else
        {
          string hier = string.Join( ">", element.ProjectHierarchy.ToArray() );
          Debug.Log( Indent + Node.Text + "(" + hier + ")" );
        }
        foreach ( TreeNode subNode in Node.Nodes )
        {
          DumpElementHierarchy( subNode, Indent + " " );
        }
      }
    }



    private void dumpHierarchyToolStripMenuItem_Click( object sender, EventArgs e )
    {
      foreach ( TreeNode node in m_SolutionExplorer.treeProject.Nodes )
      {
        DumpElementHierarchy( node, "" );
      }
      Debug.Log( "by project:" );
      foreach ( TreeNode node in m_SolutionExplorer.treeProject.Nodes )
      {
        Project project = (Project)node.Tag;
        Debug.Log( "Project " + project.Settings.Name );

        foreach ( ProjectElement element in project.Elements )
        {
          string hier = string.Join( ">", element.ProjectHierarchy.ToArray() );
          Debug.Log( "-" + element.Name + " (" + hier + ")" );
        }
      }
    }



    private void solutionAddExistingProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      System.Windows.Forms.OpenFileDialog dlgTool = new OpenFileDialog();

      dlgTool.Filter = FilterString( Types.Constants.FILEFILTER_PROJECT );
      if ( dlgTool.ShowDialog() == DialogResult.OK )
      {
        if ( OpenProject( dlgTool.FileName ) != null )
        {
          m_CurrentProject.SetModified();
        }
      }
    }



    private void solutionCloseToolStripMenuItem_Click( object sender, EventArgs e )
    {
      CloseSolution();
    }



    private void fileCloseToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( ActiveDocument != null )
      {
        ActiveDocument.Close();
      }
    }



    private void solutionAddNewProjectToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewProjectAndOrSolution();
    }


    
    private void AddNewProjectAndOrSolution()
    {
      if ( m_Solution == null )
      {
        FormSolutionWizard solWizard = new FormSolutionWizard( "New Solution", StudioCore.Settings );
        if ( solWizard.ShowDialog() == DialogResult.OK )
        {
          try
          {
            System.IO.Directory.CreateDirectory( solWizard.SolutionPath );
          }
          catch ( System.Exception ex )
          {
            System.Windows.Forms.MessageBox.Show( "Could not create solution folder:" + System.Environment.NewLine + ex.Message, "Could not create solution folder" );
            return;
          }

          m_Solution = new Solution( this );
          m_Solution.Name = solWizard.SolutionName;
          m_Solution.Filename = solWizard.SolutionFilename;

          Project newProject = new Project();
          newProject.Core = StudioCore;
          newProject.Settings.Name = solWizard.SolutionName;
          newProject.Settings.Filename = solWizard.ProjectFilename;
          newProject.Settings.BasePath = System.IO.Path.GetDirectoryName( newProject.Settings.Filename );
          newProject.Node = new TreeNode();
          newProject.Node.Tag = newProject;
          newProject.Node.Text = newProject.Settings.Name;

          Text += " - " + newProject.Settings.Name;

          m_Solution.Projects.Add( newProject );

          m_SolutionExplorer.treeProject.Nodes.Add( newProject.Node );

          RaiseApplicationEvent( new C64Studio.Types.ApplicationEvent( C64Studio.Types.ApplicationEvent.Type.SOLUTION_OPENED ) );

          SetActiveProject( newProject );
          projectToolStripMenuItem.Visible = true;

          SaveSolution();
          SaveProject( newProject );
          UpdateUndoSettings();
        }
      }
      else
      {
        AddNewProject();
      }
    }



    private void solutionSaveToolStripMenuItem1_Click( object sender, EventArgs e )
    {
      SaveSolution();
    }



    public bool ImportImage( string Filename, GR.Image.FastImage IncomingImage, Types.GraphicType ImportType, Types.MulticolorSettings MCSettings, out GR.Image.FastImage MappedImage, out Types.MulticolorSettings NewMCSettings, out bool PasteAsBlock )
    {
      PasteAsBlock = false;
      // shortcut possible? (check if palette matches ours)
      if ( IncomingImage == null )
      {
        System.Drawing.Bitmap bmpImage = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile( Filename );
        IncomingImage = GR.Image.FastImage.FromImage( bmpImage );
        bmpImage.Dispose();
      }

      MappedImage = null;
      if ( IncomingImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed )
      {
        // match palette
        bool match = true;
        for ( int i = 0; i < 16; ++i )
        {
          if ( ( IncomingImage.PaletteRed( i ) != ( Types.ConstantData.Palette.ColorValues[i] & 0xff0000 >> 16 ) )
          ||   ( IncomingImage.PaletteGreen( i ) != ( Types.ConstantData.Palette.ColorValues[i] & 0xff00 >> 8 ) )
          ||   ( IncomingImage.PaletteBlue( i ) != ( Types.ConstantData.Palette.ColorValues[i] & 0xff ) ) )
          {
            match = false;
            break;
          }
        }
        if ( match )
        {
          MappedImage = IncomingImage;
          NewMCSettings = MCSettings;
          return true;
        }
      }

      DlgGraphicImport importGFX = new DlgGraphicImport( StudioCore, ImportType, IncomingImage, Filename, MCSettings );
      if ( importGFX.ShowDialog() != DialogResult.OK )
      {
        IncomingImage.Dispose();
        NewMCSettings = MCSettings;
        return false;
      }
      PasteAsBlock = importGFX.PasteAsBlock;

      IncomingImage.Dispose();
      MappedImage = importGFX.ConvertedImage;
      NewMCSettings = importGFX.MultiColorSettings;
      return true;
    }



    private void licenseToolStripMenuItem_Click_1( object sender, EventArgs e )
    {
      FormLicense  form = new FormLicense();

      form.ShowDialog();
    }



    private void DumpPanel( string Indent, DockPanel Panel )
    {
      BaseDocument  dummy = m_SolutionExplorer;

      Panel.SaveAsXml( @"d:\gnu.xml" );

      Debug.Log( Indent + "Container" );
      foreach ( var docs in Panel.Documents )
      {
        if ( docs is BaseDocument )
        {
          Debug.Log( Indent + "-document " + ( (BaseDocument)docs ).Text );
        }
      }
      foreach ( var pane in Panel.Panes )
      {
        Debug.Log( Indent + "-Pane at " + pane.DockState + " is visible: " + pane.Visible + " at " + pane.Location.X + "," + pane.Location.Y );
        foreach ( BaseDocument content in pane.Contents )
        {
          if ( pane.DockState == content.DockState )
          {
            Debug.Log( Indent + " -" + content.Visible + " = " + content.Text );
            if ( content == pane.ActiveContent )
            {
              Debug.Log( Indent + " =Active" );
            }

            Form  form = content;

            while ( ( form != null )
            &&      ( form != this ) )
            {
              form = form.ParentForm;
              if ( ( form != null )
              &&   ( form != this ) )
              {
                Debug.Log( "-parented by " + form.Text + "(" + form.ToString() + ") at " + form.Location );
              }
            }
          }
          else if ( !content.Visible )
          {
            Debug.Log( Indent + " -" + content.Visible + " = " + content.Text );
          }
        }
      }
    }



    private void dumpDockStateToolStripMenuItem_Click( object sender, EventArgs e )
    {
      System.IO.MemoryStream    memOut = new System.IO.MemoryStream();

      panelMain.SaveAsXml( memOut, Encoding.UTF8 );

      GR.Memory.ByteBuffer    data = new GR.Memory.ByteBuffer( memOut.ToArray() );

      Debug.Log( data.ToString() );
      //DumpPanel( "", panelMain );

      /*
      foreach ( var tool in Settings.Tools )
      {
        Form  form = tool.Value.Document;


        Debug.Log( "Tool " + tool.Key + " is visible:" + tool.Value.Document.Visible );
        Debug.Log( " at " + tool.Value.Document.DockState + " at " + tool.Value.Document.Location.X + "," + tool.Value.Document.Location.Y );
        while ( ( form != null )
        &&      ( form != this ) )
        {
          form = form.ParentForm;
          if ( form != null )
          {
            Debug.Log( "-parented by " + form.Text );
          }
        }
      }*/
    }



    public void CloseAllDocuments()
    {
      if ( panelMain.DocumentStyle == DocumentStyle.SystemMdi )
      {
        foreach ( Form form in MdiChildren )
        {
          form.Close();
        }
      }
      else
      {
        foreach ( IDockContent document in panelMain.DocumentsToArray() )
        {
          document.DockHandler.Close();
        }
      }
    }



    private void runTestsToolStripMenuItem_Click( object sender, EventArgs e )
    {
      UnitTests.TestManager   manager = new C64Studio.UnitTests.TestManager( this );

      manager.RunTests();
    }



    private void fileNewSolutionToolStripMenuItem_Click( object sender, EventArgs e )
    {
      CloseSolution();
      AddNewProjectAndOrSolution();
    }



    private void mainToolRebuild_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.REBUILD );
    }



    private void charsetScreenToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.CHARACTER_SCREEN, "Character Screen", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void graphicScreenToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.GRAPHIC_SCREEN, "Graphic Screen", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void mapToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewDocumentOrElement( ProjectElement.ElementType.MAP_EDITOR, "Map Project", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void solutionToolStripMenuItem1_Click( object sender, EventArgs e )
    {
      AddNewProjectAndOrSolution();
    }



    private void projectAddNewASMFileToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewElement( ProjectElement.ElementType.ASM_SOURCE, "ASM File", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void projectAddNewBASICFileToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewElement( ProjectElement.ElementType.BASIC_SOURCE, "BASIC File", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void projectAddNewSpriteSetToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewElement( ProjectElement.ElementType.SPRITE_SET, "Sprite Set", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void projectAddNewCharacterSetToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewElement( ProjectElement.ElementType.CHARACTER_SET, "Character Set", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void projectAddNewCharacterScreenToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewElement( ProjectElement.ElementType.CHARACTER_SCREEN, "Charset Screen", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void projectAddNewGraphicScreenToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewElement( ProjectElement.ElementType.GRAPHIC_SCREEN, "Graphic Screen", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void projectAddNewMapToolStripMenuItem_Click( object sender, EventArgs e )
    {
      AddNewElement( ProjectElement.ElementType.ASM_SOURCE, "ASM File", m_CurrentProject, ( m_CurrentProject != null ) ? m_CurrentProject.Node : null );
    }



    private void editToolStripMenuItem_DropDownOpening( object sender, EventArgs e )
    {
      UpdateUndoSettings();
    }



    private void cutToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( ActiveDocument != null )
      {
        ActiveDocument.Cut();
        UpdateUndoSettings();
      }
    }



    private void copyToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( ActiveDocument != null )
      {
        ActiveDocument.Copy();
        UpdateUndoSettings();
      }
    }



    private void pasteToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( ActiveDocument != null )
      {
        ActiveDocument.Paste();
        UpdateUndoSettings();
      }
    }



    private void deleteToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( ActiveDocument != null )
      {
        ActiveDocument.Delete();
        UpdateUndoSettings();
      }
    }



    private void mainToolToggleTrueDrive_Click( object sender, EventArgs e )
    {
      StudioCore.Settings.TrueDriveEnabled = !StudioCore.Settings.TrueDriveEnabled;
      if ( StudioCore.Settings.TrueDriveEnabled )
      {
        mainToolToggleTrueDrive.Image = Properties.Resources.toolbar_truedrive_enabled;
      }
      else
      {
        mainToolToggleTrueDrive.Image = Properties.Resources.toolbar_truedrive_disabled;
      }
    }



    private void mainToolEmulator_SelectedIndexChanged( object sender, EventArgs e )
    {
      if ( mainToolEmulator.SelectedIndex == -1 )
      {
        StudioCore.Settings.EmulatorToRun = "";
      }
      else
      {
        StudioCore.Settings.EmulatorToRun = ( (GR.Generic.Tupel<string, ToolInfo>)mainToolEmulator.SelectedItem ).first.ToUpper();
      }
    }



    private void mainToolOpenFile_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.OPEN_FILES );
    }



    private void mainToolCommentSelection_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.COMMENT_SELECTION );
    }



    private void mainToolUncommentSelection_Click( object sender, EventArgs e )
    {
      ApplyFunction( C64Studio.Types.Function.UNCOMMENT_SELECTION );
    }



    internal void RefreshGUIColors()
    {
      mainMenu.BackColor = System.Drawing.Color.FromArgb( (int)StudioCore.Settings.SyntaxColoring[Types.ColorableElement.BACKGROUND_CONTROL].BGColor );

      Invalidate();
    }



    private void markErrorToolStripMenuItem_Click( object sender, EventArgs e )
    {
      if ( m_ActiveSource != null )
      {
        SourceASMEx   source = m_ActiveSource as SourceASMEx;

        source.MarkTextAsError( 1, 2, 5 );
      }
    }



    private void throwExceptionToolStripMenuItem_Click( object sender, EventArgs e )
    {
      throw new Exception( "oh the noes" );
    }



    internal string GetDocumentInfoText( DocumentInfo DocInfo )
    {
      string elementPath = "";
      if ( System.IO.Path.IsPathRooted( DocInfo.FullPath ) )
      {
        elementPath = DocInfo.FullPath;
      }
      else if ( DocInfo.Project != null )
      {
        elementPath = GR.Path.Normalize( GR.Path.Append( DocInfo.Project.Settings.BasePath, DocInfo.FullPath ), false );
      }
      else
      {
        elementPath = DocInfo.FullPath;
      }

      if ( DocInfo.BaseDoc != null )
      {
        if ( DocInfo.BaseDoc is SourceASMEx )
        {
          DateTime    lastModificationTimeStamp = ( (SourceASMEx)DocInfo.BaseDoc ).LastChange;

          if ( ( GR.Path.IsPathEqual( StudioCore.Searching.PreviousSearchedFile, elementPath ) )
          &&   ( lastModificationTimeStamp <= StudioCore.Searching.PreviousSearchedFileTimeStamp ) )
          {
            return StudioCore.Searching.PreviousSearchedFileContent;
          }
          StudioCore.Searching.PreviousSearchedFile = elementPath;
          StudioCore.Searching.PreviousSearchedFileTimeStamp = lastModificationTimeStamp;
          StudioCore.Searching.PreviousSearchedFileContent = ( (SourceASMEx)DocInfo.BaseDoc ).editSource.Text;
          return StudioCore.Searching.PreviousSearchedFileContent;
        }
        else if ( DocInfo.BaseDoc is SourceBasicEx )
        {
          StudioCore.Searching.PreviousSearchedFile = elementPath;
          return ( (SourceBasicEx)DocInfo.BaseDoc ).editSource.Text;
        }
        else if ( DocInfo.BaseDoc is Disassembler )
        {
          StudioCore.Searching.PreviousSearchedFile = elementPath;
          return ( (Disassembler)DocInfo.BaseDoc ).editDisassembly.Text;
        }
        return "";
      }

      // can we use cached text?
      bool    cacheIsUpToDate = false;

      DateTime    lastAccessTimeStamp;

      try
      {
        lastAccessTimeStamp = System.IO.File.GetLastWriteTime( elementPath );

        cacheIsUpToDate = ( lastAccessTimeStamp <= StudioCore.Searching.PreviousSearchedFileTimeStamp );

        StudioCore.Searching.PreviousSearchedFileTimeStamp = lastAccessTimeStamp;
      }
      catch ( Exception )
      {
      }

      if ( ( GR.Path.IsPathEqual( StudioCore.Searching.PreviousSearchedFile, elementPath ) )
      &&   ( cacheIsUpToDate )
      &&   ( StudioCore.Searching.PreviousSearchedFileContent != null ) )
      {
        return StudioCore.Searching.PreviousSearchedFileContent;
      }

      StudioCore.Searching.PreviousSearchedFileContent = GR.IO.File.ReadAllText( elementPath );
      StudioCore.Searching.PreviousSearchedFile = elementPath;
      return StudioCore.Searching.PreviousSearchedFileContent;
    }

  }
}