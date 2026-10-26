using System.Collections.Generic;

namespace Lumen.Core
{
    /// <summary>
    /// ES/EN text table. Spanish is neutral Latin American ("tú"). Menus and titles are uppercase;
    /// longer texts use sentence case (the pixel font has lowercase glyphs).
    /// </summary>
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
            ["difficulty.title"] = ("ELIGE TU FUEGO", "CHOOSE YOUR FIRE"),
            ["difficulty.vela"] = ("VELA", "CANDLE"),
            ["difficulty.antorcha"] = ("ANTORCHA", "TORCH"),
            ["difficulty.hoguera"] = ("HOGUERA", "BONFIRE"),
            ["difficulty.ceniza"] = ("CENIZA", "ASH"),
            ["difficulty.vela.desc"] = ("Para aprender. Las sombras son lentas y el fuego dura más.", "To learn. Shades are slow and fire lasts longer."),
            ["difficulty.antorcha.desc"] = ("La experiencia pensada. Cada decisión cuenta.", "The intended experience. Every choice matters."),
            ["difficulty.hoguera.desc"] = ("Sombras rápidas, golpes duros y ninguna flecha que te guíe.", "Fast shades, hard hits and no arrow to guide you."),
            ["difficulty.ceniza.desc"] = ("Pesadilla. Te ven desde lejos y el Devorador mata de un golpe.", "Nightmare. They see you from afar; the Devourer kills in one hit."),
            ["difficulty.record"] = ("RÉCORD", "BEST"),
            ["difficulty.norecord"] = ("Todavía sin récord", "No record yet"),
            ["title.floor"] = ("PISO", "FLOOR"),
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
            ["settings.on"] = ("SÍ", "ON"),
            ["settings.off"] = ("NO", "OFF"),
            ["pause.title"] = ("PAUSA", "PAUSED"),

            // ---- the book (tutorial): fragments of a journal left by someone who came down before
            ["book.hint"] = ("< >  PASAR PÁGINA      ESC: CERRAR", "< >  TURN PAGE      ESC: CLOSE"),
            ["book.last"] = ("ENTER: COMENZAR", "ENTER: BEGIN"),
            ["book.1.title"] = ("TU LLAMA", "YOUR FLAME"),
            ["book.1.body"] = ("Eres una llama pequeña en un lugar que olvidó la luz. Tu fuego es tu vida, tus ojos y tu reloj: se consume aunque no hagas nada. Cuando se apague, todo habrá terminado.", "You are a small flame in a place that forgot the light. Your fire is your life, your eyes and your clock: it burns down even if you do nothing. When it goes out, it is over."),
            ["book.2.title"] = ("LOS BRASEROS", "THE BRAZIERS"),
            ["book.2.body"] = ("Los antiguos dejaron braseros en cada nivel. Enciéndelos todos y la salida se abrirá. Junto a su fuego recuperas fuerzas, pero cada brasero guarda poco: cuando se seca, ya no da más.", "The old ones left braziers on every level. Light them all and the exit will open. By their fire you recover, but each holds little: once dry, it gives no more."),
            ["book.3.title"] = ("ATENUAR Y CORRER", "DIM AND RUN"),
            ["book.3.body"] = ("Mantén Ctrl para atenuarte: casi nadie te verá y gastarás menos, aunque avanzarás despacio.\nMantén Shift para correr: llegarás antes, pero arderás el triple y tu brillo te delatará.", "Hold Ctrl to dim yourself: almost nothing will see you and you will burn less, though you move slowly.\nHold Shift to run: you will arrive sooner, but burn three times as fast and your glow will give you away."),
            ["book.4.title"] = ("EL DESTELLO", "THE FLARE"),
            ["book.4.body"] = ("Con Espacio liberas un estallido de luz. Calcina lo que está cerca (anillo amarillo) y aturde lo que está más lejos (anillo naranja). Cuesta parte de tu fuego y tarda en volver.", "Space releases a burst of light. It sears what is close (yellow ring) and stuns what is farther away (orange ring). It costs part of your fire and takes time to return."),
            ["book.5.title"] = ("EL ACECHADOR", "THE STALKER"),
            ["book.5.body"] = ("Te encuentra por tu brillo: cuanto más intensa tu llama, desde más lejos te ve. Si lo enfrentas con la llama alta, vacila y se consume. Atenuada eres invisible para él, pero también indefensa.", "It finds you by your glow: the brighter your flame, the farther it sees you. Face it at full brightness and it falters and burns. Dimmed you are invisible to it, but also defenseless."),
            ["book.6.title"] = ("LA POLILLA", "THE MOTH"),
            ["book.6.body"] = ("Tu luz la atrae desde muy lejos. Te rodea, espera su momento y se lanza a robarte fuego. Atenúate para que pierda el interés, o distráela con un brasero encendido o un señuelo.", "Your light draws it from far away. It circles you, waits for its moment and dives to steal your fire. Dim yourself so it loses interest, or distract it with a lit brazier or a lure."),
            ["book.7.title"] = ("EL DEVORADOR", "THE DEVOURER"),
            ["book.7.body"] = ("La luz no lo hiere. Busca los braseros encendidos y los apaga, y la salida vuelve a cerrarse.\nPara vencerlo necesitas tres destellos, uno a la vez. Tras cada golpe se enfurece: aléjate hasta que sus marcas vuelvan a brillar.", "Light cannot hurt it. It seeks lit braziers and snuffs them, sealing the exit again.\nTo defeat it you need three flares, one at a time. After each blow it rages: keep away until its marks shine again."),
            ["book.8.title"] = ("OBJETOS", "ITEMS"),
            ["book.8.body"] = ("Farol: detiene un golpe.\nAceite: tu próximo destello no cuesta fuego.\nSeñuelo (E): lánzalo y las sombras irán tras él.\nMapa: revela todos los braseros del nivel.", "Lantern: stops one hit.\nOil: your next flare costs no fire.\nLure (E): throw it and the shades will chase it.\nMap: reveals every brazier on the level."),
            ["book.9.title"] = ("LUGARES", "PLACES"),
            ["book.9.body"] = ("Criptas: rejas que se abren y se cierran. Cruza cuando estén arriba.\nCavernas: correr por el agua ahoga tu llama.\nRaíces: ráfagas de viento que te debilitan y te arrastran.", "Crypts: gates that open and slam shut. Cross while they are up.\nCaverns: running through water drowns your flame.\nRoots: gusts of wind that weaken you and drag you along."),
            ["book.10.title"] = ("BENDICIONES", "BLESSINGS"),
            ["book.10.body"] = ("Cada vez que desciendas podrás elegir una bendición entre tres. Se acumulan durante toda la partida, así que ninguna travesía será igual a otra.\nBaja tan hondo como puedas.", "Every time you descend you may choose one blessing out of three. They stack for the whole run, so no journey is like another.\nGo as deep as you can."),

            // ---- blessings
            ["blessing.title"] = ("ELIGE UNA BENDICIÓN", "CHOOSE A BLESSING"),
            ["blessing.hint"] = ("< >  ELEGIR      ENTER: TOMAR", "< >  SELECT      ENTER: TAKE"),
            ["blessing.wick"] = ("Mecha larga", "Long wick"),
            ["blessing.wick.desc"] = ("Tu llama guarda 20 de fuego más.", "Your flame holds 20 more fire."),
            ["blessing.cheapflare"] = ("Destello barato", "Cheap flare"),
            ["blessing.cheapflare.desc"] = ("Los destellos cuestan un 25% menos.", "Flares cost 25% less."),
            ["blessing.quickflare"] = ("Chispa veloz", "Quick spark"),
            ["blessing.quickflare.desc"] = ("El destello vuelve un 30% antes.", "The flare returns 30% sooner."),
            ["blessing.embers"] = ("Brasas generosas", "Generous embers"),
            ["blessing.embers.desc"] = ("Cada brasa da un 50% más de fuego.", "Each ember gives 50% more fire."),
            ["blessing.swift"] = ("Paso ligero", "Light step"),
            ["blessing.swift.desc"] = ("Te mueves un 12% más rápido.", "You move 12% faster."),
            ["blessing.hearth"] = ("Corazón del hogar", "Heart of the hearth"),
            ["blessing.hearth.desc"] = ("Cada brasero guarda 30 de fuego extra.", "Each brazier holds 30 extra fire."),
            ["blessing.shadow"] = ("Sombra amiga", "Friendly shadow"),
            ["blessing.shadow.desc"] = ("Atenuada gastas un 30% menos.", "Dimmed, you burn 30% less."),
            ["blessing.fierce"] = ("Llama feroz", "Fierce flame"),
            ["blessing.fierce.desc"] = ("Tu luz quema un 40% más rápido.", "Your light burns 40% faster."),
            ["blessing.owl"] = ("Paso silencioso", "Silent step"),
            ["blessing.owl.desc"] = ("Los acechadores te ven un 15% menos.", "Stalkers see you 15% less."),
            ["blessing.runner"] = ("Pulmones de fragua", "Forge lungs"),
            ["blessing.runner.desc"] = ("Correr consume un 35% menos.", "Running burns 35% less."),
            ["blessing.phoenix"] = ("Segunda chispa", "Second spark"),
            ["blessing.phoenix.desc"] = ("Si te apagas, vuelves a encenderte una vez.", "If you go out, you rekindle once."),

            // ---- HUD
            ["hud.floor"] = ("PISO", "FLOOR"),
            ["hud.braziers"] = ("BRASEROS", "BRAZIERS"),
            ["hud.exit"] = ("¡La salida está abierta!", "The exit is open!"),
            ["hud.exitShort"] = ("SALIDA ABIERTA", "EXIT OPEN"),
            ["hud.low"] = ("¡TE ESTÁS APAGANDO!", "YOU ARE FADING!"),
            ["hud.relit"] = ("Un brasero se apagó", "A brazier went out"),
            ["hud.dimmed"] = ("ATENUADA", "DIMMED"),
            ["hud.refueling"] = ("RECARGANDO", "REFUELING"),
            ["hud.sprinting"] = ("CORRIENDO", "RUNNING"),
            ["hud.revived"] = ("¡SEGUNDA CHISPA!", "SECOND SPARK!"),
            ["item.Lantern"] = ("Farol", "Lantern"),
            ["item.Lure"] = ("Señuelo", "Lure"),
            ["item.Oil"] = ("Aceite", "Oil"),
            ["item.Map"] = ("Mapa", "Map"),
            ["item.full"] = ("No puedes llevar más", "You can't carry more"),
            ["item.map.used"] = ("El mapa revela los braseros", "The map reveals the braziers"),

            // ---- contextual tutorial
            ["tut.move"] = ("Muévete con WASD o las flechas", "Move with WASD or the arrow keys"),
            ["tut.brazier"] = ("Enciende los braseros para abrir la salida. Sigue la flecha", "Light the braziers to open the exit. Follow the arrow"),
            ["tut.brazier.noarrow"] = ("Enciende los braseros para abrir la salida. Explora", "Light the braziers to open the exit. Explore"),
            ["tut.fuel"] = ("Tu luz se consume. Quédate junto al fuego para recargar", "Your light burns down. Stay by the fire to refuel"),
            ["tut.dim"] = ("¡Te vio! Mantén Ctrl para atenuarte y perderlo", "It saw you! Hold Ctrl to dim yourself and lose it"),
            ["tut.flare"] = ("Espacio: destello. Quema de cerca y aturde de lejos", "Space: flare. Burns up close, stuns from afar"),
            ["tut.depleted"] = ("Ese brasero se secó. Busca brasas en la oscuridad", "That brazier ran dry. Look for embers in the dark"),
            ["tut.devourer"] = ("Devorador: tres destellos, uno a la vez. Si se enfurece, aléjate", "Devourer: three flares, one at a time. If it rages, get away"),
            ["tut.sprint"] = ("Mantén Shift para correr. Llegas antes, pero ardes el triple", "Hold Shift to run. Faster, but you burn three times as fast"),
            ["tut.item"] = ("Pulsa E para lanzar el señuelo y distraer a las sombras", "Press E to throw the lure and distract the shades"),

            // ---- game over
            ["over.title"] = ("TE EXTINGUISTE", "EXTINGUISHED"),
            ["over.score"] = ("PUNTAJE", "SCORE"),
            ["over.floor"] = ("PISO ALCANZADO", "FLOOR REACHED"),
            ["over.record"] = ("¡NUEVO RÉCORD!", "NEW RECORD!"),
            ["over.retry"] = ("R: OTRA VEZ    ESC: MENÚ", "R: RETRY    ESC: MENU"),
            ["over.time"] = ("TIEMPO", "TIME"),
            ["cause.fuel"] = ("Tu llama se consumió", "Your flame burned out"),
            ["cause.Stalker"] = ("Un acechador te alcanzó", "A stalker caught you"),
            ["cause.Moth"] = ("Las polillas devoraron tu luz", "Moths ate your light"),
            ["cause.Devourer"] = ("El Devorador te tragó", "The Devourer swallowed you"),

            // ---- floors
            ["floor.enter"] = ("PISO", "FLOOR"),
            ["floor.name.1"] = ("EL UMBRAL", "THE THRESHOLD"),
            ["floor.sub.1"] = ("La última luz que entró aquí no volvió a salir", "The last light that came in never came back out"),
            ["floor.name.2"] = ("CRIPTAS DEL POLVO", "CRYPTS OF DUST"),
            ["floor.sub.2"] = ("Algo aletea donde no debería haber aire", "Something flutters where there should be no air"),
            ["floor.name.3"] = ("LA GARGANTA", "THE GULLET"),
            ["floor.sub.3"] = ("Hay algo abajo que tiene hambre de fuego", "Something below hungers for fire"),
            ["floor.sub.Crypts"] = ("Las rejas recuerdan a quienes encerraron", "The gates remember who they locked away"),
            ["floor.sub.Flooded"] = ("El agua negra no perdona a quien corre", "Black water does not forgive the hasty"),
            ["floor.sub.Roots"] = ("El viento sopla desde donde no hay cielo", "The wind blows from where there is no sky"),
            ["floor.adj.Crypts"] = ("DEL OLVIDO|DE LOS SIN NOMBRE|DEL SILENCIO|DE HUESO|DE LAS VELAS MUERTAS", "OF OBLIVION|OF THE NAMELESS|OF SILENCE|OF BONE|OF DEAD CANDLES"),
            ["floor.noun.Crypts"] = ("CRIPTAS|OSARIO|GALERÍAS|CLAUSTRO|NICHOS", "CRYPTS|OSSUARY|GALLERIES|CLOISTER|NICHES"),
            ["floor.adj.Flooded"] = ("DE LOS AHOGADOS|DE LA MAREA|DEL LAGO NEGRO|DE LAS PROFUNDIDADES|DE LAS GOTAS", "OF THE DROWNED|OF THE TIDE|OF THE BLACK LAKE|OF THE DEEP|OF DRIPPING WATER"),
            ["floor.noun.Flooded"] = ("CAVERNAS|POZOS|CISTERNAS|GRUTAS|ACUEDUCTOS", "CAVERNS|WELLS|CISTERNS|GROTTOS|AQUEDUCTS"),
            ["floor.adj.Roots"] = ("DE LA RAÍZ MADRE|DEL VIENTO|DE LAS ESPINAS|DE LA SAVIA|DE LOS SUSURROS", "OF THE MOTHER ROOT|OF WIND|OF THORNS|OF SAP|OF WHISPERS"),
            ["floor.noun.Roots"] = ("BOSQUE|NIDO|ENTRAÑAS|MARAÑA|HONDONADA", "FOREST|NEST|ENTRAILS|THICKET|HOLLOW"),
        };

        public static bool English => SaveData.Language == "en";

        public static string Get(string key)
        {
            if (!Table.TryGetValue(key, out var v)) return key.ToUpperInvariant();
            return English ? v.en : v.es;
        }

        public static bool Has(string key) => Table.ContainsKey(key);

        public static void Toggle() => SaveData.Language = English ? "es" : "en";

        /// <summary>Adds entries from feature modules (characters, shop, enemies...) without editing this file.</summary>
        public static void Register(string key, string es, string en) => Table[key] = (es, en);
    }
}
