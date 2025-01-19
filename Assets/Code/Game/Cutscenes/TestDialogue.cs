using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDialogue : GfgVnScene
{
    protected override void Begin()
    {
        LoadScene(GfcSceneId.ENV_MUSUEM);

        Say("I suck dick", new(GfcStoryCharacter.PROTAG));
        Say("Neat trick, right?");

        Say("I am gay", new CornDialogueSetting(GfcStoryCharacter.GF));

        SayUntranslated("............Well... this is awkward...", new CornDialogueSetting(GfcStoryCharacter.GF));

        SayKey("MuhDickTest", new(GfcStoryCharacter.GF));
        {
            Append(" Cool dick");

            Option("Bruh", Test1Num, false, "Neahh shit was too long");
            Option("Bruh", Test2Num);
            Option("Bruh", Test3Num);
            {
                Append(" neah dick too small to press this");
            }
        }

        Wait(1.0f);

        Say("Pretty cool choice...");
        Say("Pretty cool choice1...");
        Say("Pretty cool choice2...");
        Say("Pretty cool choice3...");
        Say("Pretty cool choice4...");
    }

    void Test1Num()
    {
        LoadScene(GfcSceneId.ENV_PARK);

        Say("Bruh", GfcStoryCharacter.GF);
        SayKey("Lmao");

        Say("Bruh", GfcStoryCharacter.DUNNO);
        Option("Yuh", Test2Num);
    }

    void Test2Num()
    {
        Say("whack");
        Next(Test3Num);
    }

    void Test3Num()
    {
        LoadScene(GfcSceneId.ENV_RESTAURANT);
        Say("non", new CornDialogueSetting(GfcStoryCharacter.TEST));
        Say("eheh");
    }
}