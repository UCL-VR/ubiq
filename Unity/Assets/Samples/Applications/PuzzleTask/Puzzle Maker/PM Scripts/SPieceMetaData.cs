using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PuzzleMaker
{

    public class SPieceMetaData
    {
        public SPieceInfo PieceMetaData = null;
        public Texture2D PieceImage = null;

        public SPieceMetaData(SPieceInfo MetaData, Texture2D Image)
        {
            PieceMetaData = MetaData;
            PieceImage = Image;
        }

    }

}
