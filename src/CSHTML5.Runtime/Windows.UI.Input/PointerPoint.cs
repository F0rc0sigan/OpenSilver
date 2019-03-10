﻿
//===============================================================================
//
//  IMPORTANT NOTICE, PLEASE READ CAREFULLY:
//
//  ● This code is dual-licensed (GPLv3 + Commercial). Commercial licenses can be obtained from: http://cshtml5.com
//
//  ● You are NOT allowed to:
//       – Use this code in a proprietary or closed-source project (unless you have obtained a commercial license)
//       – Mix this code with non-GPL-licensed code (such as MIT-licensed code), or distribute it under a different license
//       – Remove or modify this notice
//
//  ● Copyright 2019 Userware/CSHTML5. This code is part of the CSHTML5 product.
//
//===============================================================================


#if !MIGRATION

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.UI.Input
{
    /// <summary>
    /// Provides basic properties for the input pointer associated with a single
    /// mouse, pen/stylus, or touch contact.
    /// </summary>
    public sealed class PointerPoint
    {
        //uint _frameId;
        ///// <summary>
        ///// Gets the ID of an input frame.
        ///// </summary>
        //public uint FrameId
        //{
        //    get { return _frameId; }
        //    private set { _frameId = value; }
        //}

      
        //bool _isInContact = false;
        ///// <summary>
        ///// Gets a value that indicates whether the physical entity (touch, pen/stylus,
        ///// or mouse button) is pressed down.
        ///// </summary>
        //public bool IsInContact
        //{
        //    get { return _isInContact; }
        //    private set { _isInContact = value; }
        //}


        //
        // Summary:
        //     Gets information about the device associated with the input pointer.
        //
        // Returns:
        //     The input device.
        //public PointerDevice PointerDevice { get; }
        //todo
        
        //uint _pointerId;
        ///// <summary>
        ///// Gets a unique identifier for the input pointer.
        ///// </summary>
        //public uint PointerId
        //{
        //    get { return _pointerId; }
        //    private set { _pointerId = value; }
        //}

        Point _position;
        /// <summary>
        /// Gets the location of the pointer input in client coordinates.
        /// </summary>
        public Point Position
        {
            get { return _position; }
            internal set { _position = value; }
        }

        //
        // Summary:
        //     Gets extended information about the input pointer.
        //
        // Returns:
        //     The extended properties exposed by the input device.
        //public PointerPointProperties Properties { get; }
        //todo


        //Point _rawPosition;
        ///// <summary>
        ///// Gets the raw location of the pointer input in client coordinates.
        ///// </summary>
        //public Point RawPosition
        //{
        //    get { return _rawPosition; }
        //    private set { _rawPosition = value; }
        //}

        //ulong _timestamp;
        ///// <summary>
        ///// Gets the time when the input occurred.
        ///// </summary>
        //public ulong Timestamp
        //{
        //    get { return _timestamp; }
        //    private set { _timestamp = value; }
        //}

        /*
        /// <summary>
        /// Retrieves position and state information for the specified pointer.
        /// </summary>
        /// <param name="pointerId">The id of the pointer.</param>
        /// <returns>The pointer property values.</returns>
        public static PointerPoint GetCurrentPoint(uint pointerId)
        {
            //todo: implement this method
            throw new NotImplementedException();
        }

        //
        // Summary:
        //     Retrieves the transformed information for the specified pointer.
        //
        // Parameters:
        //   pointerId:
        //     The of the pointer.
        //
        //   transform:
        //     The transform to apply to the pointer.
        //
        // Returns:
        //     The pointer property values.
        [Overload("GetCurrentPointTransformed")]
        public static PointerPoint GetCurrentPoint(uint pointerId, IPointerPointTransform transform);
        //
        // Summary:
        //     Retrieves position and state information for the specified pointer, from
        //     the last pointer event up to and including the current pointer event.
        //
        // Parameters:
        //   pointerId:
        //     The of the pointer.
        //
        // Returns:
        //     The transformed pointer properties (current and historic).
        [Overload("GetIntermediatePoints")]
        public static IList<PointerPoint> GetIntermediatePoints(uint pointerId);
        //
        // Summary:
        //     Retrieves the transformed position and state information for the specified
        //     pointer, from the last pointer event up to and including the current pointer
        //     event.
        //
        // Parameters:
        //   pointerId:
        //     The of the pointer.
        //
        //   transform:
        //     The transform to apply to the pointer.
        //
        // Returns:
        //     The transformed pointer properties (current and historic).
        [Overload("GetIntermediatePointsTransformed")]
        public static IList<PointerPoint> GetIntermediatePoints(uint pointerId, IPointerPointTransform transform);
         * */
    }
}
#endif