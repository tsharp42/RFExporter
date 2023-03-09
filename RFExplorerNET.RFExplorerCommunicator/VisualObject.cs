using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFExplorerNET.RFExplorerCommunicator
{
    /// <summary>
    /// This class is used to store information about objects which are used on Zedgraph to draw text (in a future other things)
    /// </summary>
    public class VisualObject : Object
    {
        float m_fX, m_fY;
        string m_sText;

        /// <summary>
        /// Set or Get X axis coordinate of user defined text
        /// </summary>
        public float X
        {
            get
            {
                return m_fX;
            }

            set
            {
                m_fX = value;
            }
        }

        /// <summary>
        /// Set or Get Y axis coordinate of user defined text
        /// </summary>
        public float Y
        {
            get
            {
                return m_fY;
            }

            set
            {
                m_fY = value;
            }
        }

        /// <summary>
        /// Get or Set user defined text
        /// </summary>
        public string Text
        {
            get
            {
                return m_sText;
            }

            set
            {
                m_sText = value;
            }
        }

        enum eVisualObjectType { TEXT_OBJ, NONE }

        /// <summary>
        /// Store chart position and text defined by a user
        /// </summary>
        /// <param name="fX">X axis of the user defined text position</param>
        /// <param name="fY">Y axis of the user defined text position</param>
        /// <param name="sText">user defined text</param>
        public VisualObject(float fX, float fY, string sText)
        {
            X = fX;
            m_fY = fY;
            m_sText = sText;
        }
    }
}
