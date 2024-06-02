using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDialogue : GfgVnScene
{
    static void Start()
    {
        Environment(GfcScene.ENV_MUSUEM);

        Say("I suck dick", new(StoryCharacter.PROTAG));

        Say(new CornDialogueSetting(StoryCharacter.GF));
        {
            Append(" ");
            Append();
        }

        Say("............Well... this is awkward...", new CornDialogueSetting(StoryCharacter.GF));
        {
            Append(" ");
            Append();
        }

        SayKey("MuhDickTest", new(StoryCharacter.GF));
        {
            Append(" Cool dick");

            Option(Test1Num);
            Option(Test2Num);
            Option(Test3Num, true);
            {
                Append(" neah dick too small to press this");
            }
        }

        Wait(1.0f);

        Say("Pretty cool choice...");
    }

    static void Test1Num()
    {
        Environment(GfcScene.ENV_PARK);

        Say(new CornDialogueSetting(StoryCharacter.GF));
        SayKey("Lmao", new CornDialogueSetting(StoryCharacter.GF));
        Say(new CornDialogueSetting(StoryCharacter.DUNNO));
        Option(Test2Num);
    }

    static void Test2Num()
    {
        Say();
        Next(Test3Num);
    }

    static void Test3Num()
    {
        Environment(GfcScene.ENV_RESTAURANT);

        Say(new CornDialogueSetting(StoryCharacter.TEST));
        Say(new CornDialogueSetting(StoryCharacter.TEST));
    }
}
