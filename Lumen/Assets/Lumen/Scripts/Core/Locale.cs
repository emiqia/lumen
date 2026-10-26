using System.Collections.Generic;

namespace Lumen.Core
{
    /// <summary>Minimal ES/EN localization table.</summary>
    public static class Locale
    {
        static readonly Dictionary<string, (string es, string en)> Table = new Dictionary<string, (string, string)>
        {
            ["title.subtitle"] = ("TU LUZ ES TU VIDA", "YOUR LIGHT IS YOUR LIFE"),
            ["title.start"] = ("ENTER PARA JUGAR", "PRESS ENTER TO PLAY"),
            ["title.lang"] = ("L: ENGLISH", "L: ESPAÑOL"),
            ["title.best"] = ("RÉCORD", "BEST"),
            ["title.floor"] = ("PISO", "FLOOR"),
            ["title.controls"] = ("WASD MOVER   SHIFT ATENUAR   ESPACIO DESTELLO", "WASD MOVE   SHIFT DIM   SPACE FLARE"),
            ["title.quit"] = ("ESC: SALIR", "ESC: QUIT"),

            ["hud.floor"] = ("PISO", "FLOOR"),
            ["hud.braziers"] = ("BRASEROS", "BRAZIERS"),
            ["hud.exit"] = ("¡LA SALIDA ESTÁ ABIERTA!", "THE EXIT IS OPEN!"),
            ["hud.low"] = ("¡TE ESTÁS APAGANDO!", "YOU ARE FADING!"),
            ["hud.relit"] = ("¡UN BRASERO SE APAGÓ!", "A BRAZIER WENT OUT!"),

            ["tut.move"] = ("WASD / FLECHAS PARA MOVERTE", "WASD / ARROWS TO MOVE"),
            ["tut.brazier"] = ("ENCIENDE TODOS LOS BRASEROS PARA ABRIR LA SALIDA", "LIGHT EVERY BRAZIER TO OPEN THE EXIT"),
            ["tut.fuel"] = ("TU LUZ SE CONSUME. LOS BRASEROS TE RECARGAN", "YOUR LIGHT BURNS OUT. BRAZIERS REFUEL YOU"),
            ["tut.dim"] = ("SHIFT: ATENÚA TU LUZ PARA PASAR DESAPERCIBIDO", "SHIFT: DIM YOUR LIGHT TO SNEAK PAST"),
            ["tut.flare"] = ("ESPACIO: DESTELLO, QUEMA SOMBRAS PERO CUESTA LUZ", "SPACE: FLARE BURNS SHADES BUT COSTS LIGHT"),

            ["pause.title"] = ("PAUSA", "PAUSED"),
            ["pause.resume"] = ("ESC: CONTINUAR", "ESC: RESUME"),
            ["pause.quit"] = ("Q: MENÚ PRINCIPAL", "Q: MAIN MENU"),
            ["pause.shake"] = ("S: TEMBLOR DE PANTALLA", "S: SCREEN SHAKE"),

            ["over.title"] = ("TE EXTINGUISTE", "EXTINGUISHED"),
            ["over.score"] = ("PUNTAJE", "SCORE"),
            ["over.floor"] = ("PISO ALCANZADO", "FLOOR REACHED"),
            ["over.record"] = ("¡NUEVO RÉCORD!", "NEW RECORD!"),
            ["over.retry"] = ("R: OTRA VEZ    ESC: MENÚ", "R: RETRY    ESC: MENU"),
            ["over.time"] = ("TIEMPO", "TIME"),

            ["cause.fuel"] = ("TU LLAMA SE CONSUMIÓ", "YOUR FLAME BURNED OUT"),
            ["cause.Stalker"] = ("UN ACECHADOR TE ALCANZÓ", "A STALKER CAUGHT YOU"),
            ["cause.Moth"] = ("LAS POLILLAS DEVORARON TU LUZ", "MOTHS ATE YOUR LIGHT"),
            ["cause.Devourer"] = ("EL DEVORADOR TE TRAGÓ", "THE DEVOURER SWALLOWED YOU"),

            ["floor.enter"] = ("PISO", "FLOOR"),
            ["floor.clear"] = ("PISO COMPLETADO", "FLOOR CLEARED"),
            ["floor.newEnemy.Moth"] = ("NUEVA AMENAZA: POLILLAS. ATRAÍDAS POR TU LUZ", "NEW THREAT: MOTHS. DRAWN TO YOUR LIGHT"),
            ["floor.newEnemy.Devourer"] = ("NUEVA AMENAZA: DEVORADOR. APAGA BRASEROS", "NEW THREAT: DEVOURER. SNUFFS BRAZIERS"),
        };

        public static bool English => SaveData.Language == "en";

        public static string Get(string key)
        {
            if (!Table.TryGetValue(key, out var v)) return key.ToUpperInvariant();
            return English ? v.en : v.es;
        }

        public static void Toggle() => SaveData.Language = English ? "es" : "en";
    }
}
