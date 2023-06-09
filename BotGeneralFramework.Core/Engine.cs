namespace BotGeneralFramework.Core;
using BotGeneralFramework.Records.CLI;

public sealed class Engine
{
  public App app { get; private init; }
  public ConfigFile config { get; private init; }
  public Options options { get; private init; }
  public Jint.Engine jsEngine { get; private init; }
  public JSConsole console { get; private init; }

  private Jint.Native.JsValue Require(string __path__, string path)
  {
    using var engine = new Jint.Engine();
    engine.SetValue("exports", new Jint.Native.JsObject(jsEngine));
    engine.SetValue("require", (string requirePath) => Require(engine.GetValue("__path__").ToString(), requirePath));
    engine.SetValue("console", console);
    engine.SetValue("eval", (string expression) => jsEngine.Evaluate(expression));
    FileInfo module = new(
      Path.Combine(Directory.GetParent(__path__)!.FullName, path)
    );
    engine.SetValue("__path__", module.FullName);
    return engine.Execute(
      File.ReadAllText(module.FullName),
      module.FullName
    ).GetValue("exports");
  }

  public Engine(ConfigFile config, Options options)
  {
    // setup global variables
    this.app = new(
      new() {
        { "options", options },
        { "config", config }
      }
    );
    this.config = config;
    this.options = options;
    this.jsEngine = new Jint.Engine();
    this.console = new(options);

    // register all the values
    jsEngine.SetValue("console", console);
    jsEngine.SetValue("app", app);
    jsEngine.SetValue("options", options);
    jsEngine.SetValue("config", config);
    jsEngine.SetValue("require", (string requirePath) => Require(jsEngine.GetValue("__path__").ToString(), requirePath));
    jsEngine.SetValue("eval", (string path) => Require(jsEngine.GetValue("__path__").ToString(), path));
  }

  public App Run(FileInfo script)
  {
    // initialize stream
    using var stream = script.OpenText();
    // read the script
    var scriptText = stream.ReadToEnd();
    // close the stream
    stream.Close();

    // set the __path__ variable
    jsEngine.SetValue("__path__", script.FullName);
    // run the script
    jsEngine.Execute(scriptText, script.FullName);

    return app;
  }
  public ServiceConsole InitConsole() => new(options, config, app);
}