using System.Collections.Generic;

namespace Lumen.Core
{
    /// <summary>Minimal ES/EN localization table.</summary>
    public static class Locale
    {
        static readonly Dictionary<string, (string es, string en)> Table = new Dictionary<string, (string, string)>
        {
            // ---- front end
            ["menu.play"] = ("JUGAR", "PLAY"),
            ["menu.guide"] = ("GUÍA", "GUIDE"),
            ["menu.options"] = ("OPCIONES", "OPTIONS"),
            ["menu.quit"] = ("SALIR", "QUIT"),
            ["menu.back"] = ("VOLVER", "BACK"),
            ["menu.resume"] = ("CONTINUAR", "RESUME"),
            ["menu.abandon"] = ("ABANDONAR PARTIDA", "ABANDON RUN"),
            ["menu.hint"] = ("FLECHAS: ELEGIR   ENTER: ACEPTAR   ESC: VOLVER", "ARROWS: SELECT   ENTER: ACCEPT   ESC: BACK"),
            ["difficulty.title"] = ("ELEGÍ TU FUEGO", "CHOOSE YOUR FIRE"),
            ["difficulty.vela"] = ("VELA", "CANDLE"),
            ["difficulty.antorcha"] = ("ANTORCHA", "TORCH"),
            ["difficulty.hoguera"] = ("HOGUERA", "BONFIRE"),
            ["difficulty.ceniza"] = ("CENIZA", "ASH"),
            ["difficulty.vela.desc"] = ("PARA APRENDER. LAS SOMBRAS SON LENTAS Y EL FUEGO DURA MÁS.", "TO LEARN. SHADES ARE SLOW AND FIRE LASTS LONGER."),
            ["difficulty.antorcha.desc"] = ("LA EXPERIENCIA PENSADA. CADA DECISIÓN CUENTA.", "THE INTENDED EXPERIENCE. EVERY CHOICE MATTERS."),
            ["difficulty.hoguera.desc"] = ("SOMBRAS RÁPIDAS, GOLPES DUROS, BRASEROS AVAROS.", "FAST SHADES, HARD HITS, STINGY BRAZIERS."),
            ["difficulty.ceniza.desc"] = ("PESADILLA. TE VEN DE LEJOS Y EL DEVORADOR MATA DE UN GOLPE.", "NIGHTMARE. THEY SEE YOU FROM AFAR. THE DEVOURER KILLS IN ONE HIT."),
            ["difficulty.record"] = ("RÉCORD", "BEST"),
            ["difficulty.norecord"] = ("SIN RÉCORD TODAVÍA", "NO RECORD YET"),
            ["options.title"] = ("OPCIONES", "OPTIONS"),
            ["options.music"] = ("MÚSICA", "MUSIC"),
            ["options.sfx"] = ("EFECTOS", "SOUND"),
            ["options.screen"] = ("PANTALLA", "DISPLAY"),
            ["options.fullscreen"] = ("COMPLETA", "FULLSCREEN"),
            ["options.windowed"] = ("VENTANA", "WINDOWED"),
            ["options.brightness"] = ("BRILLO", "BRIGHTNESS"),
            ["options.shake"] = ("TEMBLOR", "SCREEN SHAKE"),
            ["options.colorblind"] = ("MODO DALTÓNICO", "COLORBLIND MODE"),
            ["options.timer"] = ("CRONÓMETRO", "SPEEDRUN TIMER"),
            ["options.language"] = ("IDIOMA", "LANGUAGE"),
            ["options.languageName"] = ("ESPAÑOL", "ENGLISH"),

            // ---- the book (tutorial)
            ["book.hint"] = ("< > PASAR PÁGINA     ESC: CERRAR", "< > TURN PAGE     ESC: CLOSE"),
            ["book.last"] = ("ENTER: EMPEZAR", "ENTER: BEGIN"),
            ["book.1.title"] = ("TU LLAMA", "YOUR FLAME"),
            ["book.1.body"] = ("SOS UNA LLAMA PERDIDA EN LA OSCURIDAD. TU LUZ ES TU VIDA, TU VISTA Y TU RELOJ: SE CONSUME SOLA. SI SE APAGA, SE TERMINÓ.", "YOU ARE A FLAME LOST IN THE DARK. YOUR LIGHT IS YOUR LIFE, YOUR SIGHT AND YOUR CLOCK: IT BURNS DOWN ON ITS OWN. IF IT GOES OUT, IT'S OVER."),
            ["book.2.title"] = ("BRASEROS", "BRAZIERS"),
            ["book.2.body"] = ("ENCENDÉ TODOS LOS BRASEROS PARA ABRIR LA SALIDA. CERCA DEL FUEGO RECARGÁS, PERO CADA BRASERO GUARDA POCO: CUANDO SE SECA, YA NO DA MÁS.", "LIGHT EVERY BRAZIER TO OPEN THE EXIT. NEAR A FIRE YOU REFUEL, BUT EACH ONE HOLDS LITTLE: ONCE DRY, IT GIVES NO MORE."),
            ["book.3.title"] = ("ATENUAR Y CORRER", "DIM AND RUN"),
            ["book.3.body"] = ("CTRL: ATENUÁ TU LUZ. GASTÁS POCO Y CASI NO TE VEN, PERO VAS LENTO.\nSHIFT: CORRÉ. VAS RÁPIDO, PERO ARDÉS EL TRIPLE Y TE VEN DE LEJOS.", "CTRL: DIM YOUR LIGHT. YOU BURN LITTLE AND ARE HARD TO SEE, BUT SLOW.\nSHIFT: RUN. YOU ARE FAST, BUT BURN THREE TIMES FASTER AND SHINE FAR."),
            ["book.4.title"] = ("DESTELLO", "FLARE"),
            ["book.4.body"] = ("ESPACIO: UN ESTALLIDO DE LUZ. QUEMA LO QUE ESTÁ CERCA (ANILLO AMARILLO) Y ATURDE LO QUE ESTÁ LEJOS (ANILLO NARANJA). CUESTA LUZ Y TARDA EN VOLVER.", "SPACE: A BURST OF LIGHT. IT BURNS WHAT IS CLOSE (YELLOW RING) AND STUNS WHAT IS FAR (ORANGE RING). IT COSTS LIGHT AND TAKES TIME TO RETURN."),
            ["book.5.title"] = ("ACECHADOR", "STALKER"),
            ["book.5.body"] = ("CUANTO MÁS BRILLÁS, DE MÁS LEJOS TE VE. SI TE QUEDÁS FIRME CON LA LLAMA ALTA, SE FRENA Y SE QUEMA. ATENUADA SOS INVISIBLE, PERO INDEFENSA.", "THE BRIGHTER YOU SHINE, THE FARTHER IT SEES YOU. STAND FIRM AT FULL BRIGHTNESS AND IT FALTERS AND BURNS. DIMMED YOU ARE INVISIBLE, BUT DEFENSELESS."),
            ["book.6.title"] = ("POLILLA", "MOTH"),
            ["book.6.body"] = ("TU LUZ LA ATRAE DESDE LEJOS. TE RODEA Y SE LANZA A ROBARTE FUEGO. ATENUÁ PARA QUE PIERDA INTERÉS, O DISTRAELA CON UN BRASERO O UN SEÑUELO.", "YOUR LIGHT DRAWS IT FROM AFAR. IT CIRCLES YOU AND DIVES TO STEAL FIRE. DIM SO IT LOSES INTEREST, OR DISTRACT IT WITH A BRAZIER OR A LURE."),
            ["book.7.title"] = ("DEVORADOR", "DEVOURER"),
            ["book.7.body"] = ("LA LUZ NO LO DAÑA. APAGA BRASEROS Y VUELVE A CERRAR LA SALIDA.\nPARA VENCERLO: 3 DESTELLOS, UNO POR VEZ. TRAS CADA UNO SE ENFURECE Y TE PERSIGUE: ALEJATE HASTA QUE SUS MARCAS VUELVAN A BRILLAR.", "LIGHT CAN'T HURT IT. IT SNUFFS BRAZIERS AND SEALS THE EXIT AGAIN.\nTO BEAT IT: 3 FLARES, ONE AT A TIME. AFTER EACH ONE IT RAGES AND CHASES YOU: KEEP AWAY UNTIL ITS MARKS SHINE AGAIN."),
            ["book.8.title"] = ("OBJETOS", "ITEMS"),
            ["book.8.body"] = ("FAROL: FRENA UN GOLPE.\nACEITE: UN DESTELLO GRATIS.\nSEÑUELO (E): TIRALO Y ATRAE A LAS SOMBRAS.\nMAPA: MARCA TODOS LOS BRASEROS DEL PISO.", "LANTERN: BLOCKS ONE HIT.\nOIL: ONE FREE FLARE.\nLURE (E): THROW IT TO DRAW SHADES AWAY.\nMAP: MARKS EVERY BRAZIER ON THE FLOOR."),
            ["book.9.title"] = ("LUGARES", "PLACES"),
            ["book.9.body"] = ("CRIPTAS: REJAS QUE SE CIERRAN. CRUZÁ CUANDO SE ABREN.\nCAVERNAS: CORRER POR EL AGUA TE APAGA.\nRAÍCES: RÁFAGAS DE VIENTO QUE TE ACHICAN LA LUZ Y TE EMPUJAN.", "CRYPTS: GATES THAT SLAM SHUT. CROSS WHEN THEY OPEN.\nCAVERNS: RUNNING THROUGH WATER DROWNS YOUR LIGHT.\nROOTS: GUSTS OF WIND SHRINK YOUR LIGHT AND PUSH YOU."),
            ["book.10.title"] = ("BENDICIONES", "BLESSINGS"),
            ["book.10.body"] = ("CADA VEZ QUE BAJÁS, ELEGÍS UNA BENDICIÓN ENTRE TRES. SE ACUMULAN TODA LA PARTIDA: CADA RUN SE ARMA DISTINTA.\nBAJÁ LO MÁS PROFUNDO QUE PUEDAS.", "EVERY TIME YOU DESCEND, CHOOSE ONE BLESSING OUT OF THREE. THEY STACK FOR THE WHOLE RUN: NO TWO RUNS ARE ALIKE.\nGO AS DEEP AS YOU CAN."),

            // ---- blessings
            ["blessing.title"] = ("ELEGÍ UNA BENDICIÓN", "CHOOSE A BLESSING"),
            ["blessing.hint"] = ("< > ELEGIR     ENTER: TOMAR", "< > SELECT     ENTER: TAKE"),
            ["blessing.wick"] = ("MECHA LARGA", "LONG WICK"),
            ["blessing.wick.desc"] = ("+20 DE LUZ MÁXIMA", "+20 MAX LIGHT"),
            ["blessing.cheapflare"] = ("DESTELLO BARATO", "CHEAP FLARE"),
            ["blessing.cheapflare.desc"] = ("LOS DESTELLOS CUESTAN 25% MENOS", "FLARES COST 25% LESS"),
            ["blessing.quickflare"] = ("CHISPA RÁPIDA", "QUICK SPARK"),
            ["blessing.quickflare.desc"] = ("EL DESTELLO VUELVE 30% ANTES", "FLARE RECHARGES 30% FASTER"),
            ["blessing.embers"] = ("BRASAS GENEROSAS", "GENEROUS EMBERS"),
            ["blessing.embers.desc"] = ("LAS BRASAS DAN 50% MÁS LUZ", "EMBERS GIVE 50% MORE LIGHT"),
            ["blessing.swift"] = ("PASO LIGERO", "LIGHT STEP"),
            ["blessing.swift.desc"] = ("TE MOVÉS 12% MÁS RÁPIDO", "MOVE 12% FASTER"),
            ["blessing.hearth"] = ("CORAZÓN DE HOGAR", "HEARTH HEART"),
            ["blessing.hearth.desc"] = ("LOS BRASEROS GUARDAN +30 DE LUZ", "BRAZIERS HOLD +30 LIGHT"),
            ["blessing.shadow"] = ("SOMBRA AMIGA", "FRIENDLY SHADOW"),
            ["blessing.shadow.desc"] = ("ATENUADA GASTÁS 30% MENOS", "DIMMED YOU BURN 30% LESS"),
            ["blessing.fierce"] = ("LLAMA FEROZ", "FIERCE FLAME"),
            ["blessing.fierce.desc"] = ("TU LUZ QUEMA 40% MÁS RÁPIDO", "YOUR LIGHT BURNS 40% FASTER"),
            ["blessing.owl"] = ("PASO DE BÚHO", "OWL STEP"),
            ["blessing.owl.desc"] = ("LOS ACECHADORES TE VEN 15% MENOS", "STALKERS SEE YOU 15% LESS"),
            ["blessing.runner"] = ("PULMONES DE FRAGUA", "FORGE LUNGS"),
            ["blessing.runner.desc"] = ("CORRER GASTA 35% MENOS", "RUNNING BURNS 35% LESS"),
            ["blessing.phoenix"] = ("SEGUNDA CHISPA", "SECOND SPARK"),
            ["blessing.phoenix.desc"] = ("SI TE APAGÁS, RENACÉS UNA VEZ", "IF YOU GO OUT, REKINDLE ONCE"),

            // ---- items and new HUD
            ["item.Lantern"] = ("FAROL", "LANTERN"),
            ["item.Lure"] = ("SEÑUELO", "LURE"),
            ["item.Oil"] = ("ACEITE", "OIL"),
            ["item.Map"] = ("MAPA", "MAP"),
            ["item.full"] = ("NO PODÉS LLEVAR MÁS", "YOU CAN'T CARRY MORE"),
            ["item.map.used"] = ("EL MAPA REVELA LOS BRASEROS", "THE MAP REVEALS THE BRAZIERS"),
            ["hud.sprinting"] = ("CORRIENDO", "RUNNING"),
            ["hud.revived"] = ("¡SEGUNDA CHISPA!", "SECOND SPARK!"),
            ["tut.devourer"] = ("DEVORADOR: 3 DESTELLOS, UNO POR VEZ. SI SE ENFURECE, ALEJATE", "DEVOURER: 3 FLARES, ONE AT A TIME. IF IT RAGES, GET AWAY"),
            ["tut.sprint"] = ("SHIFT: CORRÉ. MÁS RÁPIDO, PERO ARDÉS EL TRIPLE", "SHIFT: RUN. FASTER, BUT YOU BURN THREE TIMES AS FAST"),
            ["tut.item"] = ("E: TIRÁ EL SEÑUELO PARA DISTRAER A LAS SOMBRAS", "E: THROW THE LURE TO DISTRACT THE SHADES"),

            // ---- floor names
            ["floor.name.1"] = ("EL UMBRAL", "THE THRESHOLD"),
            ["floor.sub.1"] = ("LA ÚLTIMA LUZ QUE ENTRÓ AQUÍ NO VOLVIÓ A SALIR", "THE LAST LIGHT THAT CAME IN NEVER CAME BACK OUT"),
            ["floor.name.2"] = ("CRIPTAS DEL POLVO", "CRYPTS OF DUST"),
            ["floor.sub.2"] = ("ALGO ALETEA DONDE NO DEBERÍA HABER AIRE", "SOMETHING FLUTTERS WHERE THERE SHOULD BE NO AIR"),
            ["floor.name.3"] = ("LA GARGANTA", "THE GULLET"),
            ["floor.sub.3"] = ("HAY ALGO ABAJO QUE TIENE HAMBRE DE FUEGO", "SOMETHING BELOW HUNGERS FOR FIRE"),
            ["floor.sub.Crypts"] = ("LAS REJAS RECUERDAN A QUIENES ENCERRARON", "THE GATES REMEMBER WHO THEY LOCKED AWAY"),
            ["floor.sub.Flooded"] = ("EL AGUA NEGRA NO PERDONA A QUIEN CORRE", "BLACK WATER DOES NOT FORGIVE THE HASTY"),
            ["floor.sub.Roots"] = ("EL VIENTO SOPLA DESDE DONDE NO HAY CIELO", "THE WIND BLOWS FROM WHERE THERE IS NO SKY"),
            ["floor.adj.Crypts"] = ("DEL OLVIDO|DE LOS SIN NOMBRE|DEL SILENCIO|DE HUESO|DE LAS VELAS MUERTAS", "OF OBLIVION|OF THE NAMELESS|OF SILENCE|OF BONE|OF DEAD CANDLES"),
            ["floor.noun.Crypts"] = ("CRIPTAS|OSARIO|GALERÍAS|CLAUSTRO|NICHOS", "CRYPTS|OSSUARY|GALLERIES|CLOISTER|NICHES"),
            ["floor.adj.Flooded"] = ("DE LOS AHOGADOS|DE LA MAREA|DEL LAGO NEGRO|DE LAS PROFUNDIDADES|DE LAS GOTAS", "OF THE DROWNED|OF THE TIDE|OF THE BLACK LAKE|OF THE DEEP|OF DRIPPING WATER"),
            ["floor.noun.Flooded"] = ("CAVERNAS|POZOS|CISTERNAS|GRUTAS|ACUEDUCTOS", "CAVERNS|WELLS|CISTERNS|GROTTOS|AQUEDUCTS"),
            ["floor.adj.Roots"] = ("DE LA RAÍZ MADRE|DEL VIENTO|DE LAS ESPINAS|DE LA SAVIA|DE LOS SUSURROS", "OF THE MOTHER ROOT|OF WIND|OF THORNS|OF SAP|OF WHISPERS"),
            ["floor.noun.Roots"] = ("BOSQUE|NIDO|ENTRAÑAS|MARAÑA|HONDONADA", "FOREST|NEST|ENTRAILS|THICKET|HOLLOW"),

            ["title.start"] = ("ENTER PARA JUGAR", "PRESS ENTER TO PLAY"),
            ["title.lang"] = ("L: ENGLISH", "L: ESPAÑOL"),
            ["title.best"] = ("RÉCORD", "BEST"),
            ["title.floor"] = ("PISO", "FLOOR"),
            ["title.controls"] = ("WASD MOVER   SHIFT ATENUAR   ESPACIO DESTELLO", "WASD MOVE   SHIFT DIM   SPACE FLARE"),
            ["title.quit"] = ("ESC: SALIR", "ESC: QUIT"),
            ["settings.shake"] = ("T: TEMBLOR", "T: SHAKE"),
            ["settings.brightness"] = ("B: BRILLO", "B: BRIGHTNESS"),
            ["settings.colorblind"] = ("C: DALTÓNICO", "C: COLORBLIND"),
            ["settings.on"] = ("SÍ", "ON"),
            ["settings.off"] = ("NO", "OFF"),

            ["hud.floor"] = ("PISO", "FLOOR"),
            ["hud.braziers"] = ("BRASEROS", "BRAZIERS"),
            ["hud.exit"] = ("¡LA SALIDA ESTÁ ABIERTA!", "THE EXIT IS OPEN!"),
            ["hud.exitShort"] = ("SALIDA ABIERTA", "EXIT OPEN"),
            ["hud.low"] = ("¡TE ESTÁS APAGANDO!", "YOU ARE FADING!"),
            ["hud.relit"] = ("¡UN BRASERO SE APAGÓ!", "A BRAZIER WENT OUT!"),
            ["hud.dimmed"] = ("ATENUADA", "DIMMED"),
            ["hud.refueling"] = ("RECARGANDO", "REFUELING"),

            ["tut.move"] = ("MOVETE CON WASD O LAS FLECHAS", "WASD / ARROWS TO MOVE"),
            ["tut.brazier"] = ("ENCENDÉ LOS BRASEROS PARA ABRIR LA SALIDA. SEGUÍ LA FLECHA", "LIGHT EVERY BRAZIER TO OPEN THE EXIT. FOLLOW THE ARROW"),
            ["tut.fuel"] = ("TU LUZ SE CONSUME. QUEDATE CERCA DEL FUEGO PARA RECARGAR", "YOUR LIGHT BURNS OUT. STAY BY THE FIRE TO REFUEL"),
            ["tut.dim"] = ("¡TE VIO! SHIFT: ATENUÁ TU LUZ PARA QUE NO TE ENCUENTRE", "IT SAW YOU! SHIFT: DIM YOUR LIGHT TO HIDE"),
            ["tut.flare"] = ("ESPACIO: DESTELLO. QUEMA DE CERCA, ATURDE LEJOS. CUESTA LUZ", "SPACE: FLARE. BURNS UP CLOSE, STUNS FARTHER OUT. COSTS LIGHT"),
            ["tut.depleted"] = ("ESE BRASERO SE SECÓ Y YA NO RECARGA. JUNTÁ BRASAS", "THAT BRAZIER RAN DRY AND NO LONGER REFUELS. FIND EMBERS"),

            ["pause.title"] = ("PAUSA", "PAUSED"),
            ["pause.resume"] = ("ESC: CONTINUAR", "ESC: RESUME"),
            ["pause.quit"] = ("Q: MENÚ PRINCIPAL", "Q: MAIN MENU"),

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
            ["floor.newEnemy.Moth"] = ("NUEVA AMENAZA: POLILLAS. LAS ATRAE TU LUZ", "NEW THREAT: MOTHS. DRAWN TO YOUR LIGHT"),
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
