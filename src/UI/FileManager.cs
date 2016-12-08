﻿using System;
using System.Collections.Generic;
using UnityEngine;


namespace KERBALISM {


public sealed class FileManager : Window
{
  public FileManager()
  : base(320u, 80u, 80u, 20u, Styles.win)
  {
    // enable global access
    instance = this;

    // load textures
    send_off = Lib.GetTexture("send-black");
    send_on = Lib.GetTexture("send-cyan");
    analyze_off = Lib.GetTexture("lab-black");
    analyze_on = Lib.GetTexture("lab-cyan");
    delete = Lib.GetTexture("delete-red");

    // left icon style
    delete_icon = new GUIStyle();
    delete_icon.alignment = TextAnchor.MiddleRight;
    delete_icon.fixedWidth = 16;
    delete_icon.stretchWidth = false;

    // right icon style
    flag_icon = new GUIStyle();
    flag_icon.alignment = TextAnchor.MiddleRight;
    flag_icon.fixedWidth = 20;
    flag_icon.stretchWidth = false;
  }

  public override bool prepare()
  {
    // if there is a vessel id specified
    if (vessel_id != Guid.Empty)
    {
      // try to get the vessel
      Vessel v = FlightGlobals.Vessels.Find(k => k.id == vessel_id);

      // if the vessel doesn't exist, forget it
      if (v == null)
      {
        vessel_id = Guid.Empty;
      }
      // if the vessel is not valid, forget it
      else if (!Cache.VesselInfo(v).is_valid)
      {
        vessel_id = Guid.Empty;
      }
    }

    // if there is no vessel selected, don't draw anything
    return vessel_id != Guid.Empty;
  }


  // draw the window
  public override void render()
  {
    // get vessel
    // - the id and the vessel are valid at this point
    Vessel v = FlightGlobals.Vessels.Find(k => k.id == vessel_id);

    // get vessel drive
    Drive drive = DB.Vessel(v).drive;

    // draw pseudo-title
    if (Panel.title(v.vesselName)) Close();

    // draw data section
    Panel.section("DATA");
    foreach(var p in drive.files)
    {
      string filename = p.Key;
      File file = p.Value;
      render_file(filename, file, drive);
    }
    if (drive.files.Count == 0) Panel.content("<i>no files</i>", string.Empty);
    Panel.space();

    // draw samples section
    Panel.section("SAMPLES");
    foreach(var p in drive.samples)
    {
      string filename = p.Key;
      Sample sample = p.Value;
      render_sample(filename, sample, drive);
    }
    if (drive.samples.Count == 0) Panel.content("<i>no samples</i>", string.Empty);
    Panel.space();
  }


  void render_file(string filename, File file, Drive drive)
  {
    // start row container
    GUILayout.BeginHorizontal(Styles.entry_container);

    // render experiment name
    string exp_name = Science.experiment_name(filename);
    string exp_fullname = Science.experiment_fullname(filename);
    string exp_situation = exp_fullname.Substring(exp_name.Length).Replace("while ", string.Empty).Replace("from ", string.Empty);
    string exp_label = Lib.BuildString
    (
      "<b>",
      Lib.Ellipsis(exp_name, 16),
      "</b> <size=10>",
      Lib.Ellipsis(exp_situation, 24),
      "</size>"
    );
    string exp_tooltip = exp_fullname;
    double exp_value = Science.experiment_value(filename, file.size);
    if (exp_value > double.Epsilon) exp_tooltip = Lib.BuildString(exp_tooltip, "\n<b>", Lib.HumanReadableScience(exp_value), "</b>");
    GUILayout.Label(new GUIContent(exp_label, exp_tooltip), Styles.entry_label);

    // render data size
    GUILayout.Label(new GUIContent(Lib.HumanReadableDataSize(file.size), exp_tooltip), Styles.entry_value);

    // render flag icon
    GUILayout.Label(new GUIContent(file.send ? send_on : send_off, "Flag the file for transmission to <b>DSN</b>"), flag_icon);
    if (Lib.IsClicked()) file.send = !file.send;

    // render delete icon
    GUILayout.Label(new GUIContent(delete), delete_icon);
    if (Lib.IsClicked())
    {
      Lib.Popup
      (
        "Warning!",
        Lib.BuildString("Do you really want to delete ", Science.experiment_fullname(filename), "?"),
        new DialogGUIButton("Delete it", () => drive.files.Remove(filename)),
        new DialogGUIButton("Keep it", () => {})
      );
    }

    // end row container
    GUILayout.EndHorizontal();
  }


  void render_sample(string filename, Sample sample, Drive drive)
  {
    // start row contianer
    GUILayout.BeginHorizontal(Styles.entry_container);

    // render experiment name
    string exp_name = Science.experiment_name(filename);
    string exp_fullname = Science.experiment_fullname(filename);
    string exp_situation = exp_fullname.Substring(exp_name.Length).Replace("while ", string.Empty).Replace("from ", string.Empty);
    string exp_label = Lib.BuildString
    (
      "<b>",
      Lib.Ellipsis(exp_name, 16),
      "</b> <size=10>",
      Lib.Ellipsis(exp_situation, 24),
      "</size>"
    );
    string exp_tooltip = exp_fullname;
    double exp_value = Science.experiment_value(filename, sample.size);
    if (exp_value > double.Epsilon) exp_tooltip = Lib.BuildString(exp_tooltip, "\n<b>", Lib.HumanReadableScience(exp_value), "</b>");
    GUILayout.Label(new GUIContent(exp_label, exp_tooltip), Styles.entry_label);

    // render data size
    GUILayout.Label(new GUIContent(Lib.HumanReadableDataSize(sample.size), exp_tooltip), Styles.entry_value);

    // render flag icon
    GUILayout.Label(new GUIContent(sample.analyze ? analyze_on : analyze_off, "Flag the file for analysis in a <b>laboratory</b>"), flag_icon);
    if (Lib.IsClicked()) sample.analyze = !sample.analyze;

    // render delete icon
    GUILayout.Label(new GUIContent(delete), delete_icon);
    if (Lib.IsClicked())
    {
      Lib.Popup
      (
        "Warning!",
        Lib.BuildString("Do you really want to dump ", Science.experiment_fullname(filename), "?"),
        new DialogGUIButton("Dump it", () => drive.samples.Remove(filename)),
        new DialogGUIButton("Keep it", () => {})
      );
    }

    // end row container
    GUILayout.EndHorizontal();
  }


  public override float height()
  {
    // get vessel
    // - the id and the vessel are valid at this point, checked in on_gui()
    Vessel v = FlightGlobals.Vessels.Find(k => k.id == vessel_id);

    // get vessel drive
    Drive drive = DB.Vessel(v).drive;

    // store computed height
    float h = 20.0f;

    // files
    h += Panel.height(Math.Max(drive.files.Count, 1));

    // samples
    h += Panel.height(Math.Max(drive.samples.Count, 1));

    // finally, return the height
    return h;
  }

  // show the window
  public static void Open(Vessel v)
  {
    // setting vessel id show the window
    instance.vessel_id = v.id;
  }

  // close the window
  public static void Close()
  {
    // resetting vessel id hide the window
    instance.vessel_id = Guid.Empty;
  }

  // toggle the window
  public static void Toggle(Vessel v)
  {
    // if vessel is different, show it
    // if vessel is the same, hide it
    instance.vessel_id = (instance.vessel_id == v.id ? Guid.Empty : v.id);
  }

  // return true if the window is open
  public static bool IsOpen()
  {
    return instance.vessel_id != Guid.Empty;
  }

  // selected vessel, if any
  Guid vessel_id;

  // icons
  Texture send_off;
  Texture send_on;
  Texture analyze_off;
  Texture analyze_on;
  Texture delete;

  // styles
  GUIStyle delete_icon;
  GUIStyle flag_icon;

  // permit global access
  static FileManager instance;
}


} // KERBALISM