﻿using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapData
{
    /// <summary>
    /// A collection of vectors that surmise the surfaces on a map.
    /// you can get this either as a list of vectors or as a double array of bools, true means passable, false means impassable
    /// This will eventually be serializable to JSON
    /// </summary>
    ///<remarks>the json serializer can't handle multidimensional arrays, so that is getting spitout as a single dimension array
    ///There is a hack throughout this to copy the entire map, this is a hack to get this to work, I will figure out a better method
    ///once I have a server up and running(this means width and height MUST be exposed to reconstruct the original map)</remarks>
    [DataContract]
    public class Map
    {
        //the different internal representations of a map
        [DataMember]
        private readonly List<Line> _surfaces = new List<Line>();
        [DataMember]
        private bool[][] _pixelmap;
        [DataMember]
        private readonly int _width;
        [DataMember]
        private readonly int _height;

        /// <summary>
        /// initializes the pixel map to clear
        /// </summary>
        /// <param name="width">width of the pixel map</param>
        /// <param name="height">height of the pixel map</param>
        public Map(int width, int height)
        {
            //_pixelmap = InitializedVals(width, height);
            _width = width;
            _height = height;
            _pixelmap = InitializedVals(width, height);
        }

        /// <summary>
        /// makes a default map of height 100, width 100
        /// </summary>
        public Map()
            : this(100, 100)
        { }

        /// <summary>
        /// Clears the entire Map.
        /// </summary>
        public void ClearMap()
        {
            _surfaces.Clear();
            _pixelmap = InitializedVals(_width,_height);
            _pixelmap = InitializedVals(this.Width, this.Height);
        }

        /// <summary>
        /// constructs a new pixelmap array
        /// </summary>
        /// <param name="width">width of the array</param>
        /// <param name="height">height of the array</param>
        /// <returns>a true initialized pixel map of the map</returns>
        /// <remarks>should use an enum and should use a multi-dimensional array
        /// but the JSON serialization library is broken</remarks>
        private static bool[][] InitializedVals(int width, int height)
        {
            //build the array
            bool[][] toret = new bool[width][];

            for (int i = 0; i < height; i++)
            {
                toret[i] = new bool[height];
            }

            //initialize the array to true(default is false)
            for (int xind = 0; xind < width; xind++)
            {
                for (int yind = 0; yind < height; yind++)
                {
                    toret[xind][yind] = true;
                }
            }
            return toret;
        }


        /// <summary>
        /// Removes any manually added blocked out pixels
        /// </summary>
        public void Reinitialize()
        {
            _pixelmap = InitializedVals(_width, _height);
            foreach (Line readd in _surfaces)
            {
                ModifyArray(readd);
            }
        }

        /// <summary>
        /// Marks a point on the pixel map as impassable(false)
        /// </summary>
        /// <param name="xcoord">xcoord of impassable terrain</param>
        /// <param name="ycoord">ycoord of impassable terrain</param>
        public void MakeImpassable(int xcoord, int ycoord)
        {
            if (xcoord > (_width - 1) || ycoord > (_height - 1))
                throw new ArgumentException("invalid arguments, too large");
            _pixelmap[xcoord][ycoord] = false;
        }

        /// <summary>
        /// Marks a point on the pixel map as passable
        /// </summary>
        /// <param name="xcoord">xcoord of passable terrain</param>
        /// <param name="ycoord">ycoord of passable terrain</param>
        public void MakePassable(int xcoord, int ycoord)
        {
            if (xcoord > (_width - 1) || ycoord > (_height - 1))
                throw new ArgumentException("invalid arguments, too large");
            _pixelmap[xcoord][ycoord] = true;
        }


                

        /// <summary>
        /// returns the Surfaces as a list
        /// </summary>
        public List<Line> Surfaces
        {
            get
            {
                return _surfaces;
            }
        }

        /// <summary>
        /// returns the map as an array of boolean values from 0 to 100, 0 to 100
        /// if the value is false, the terrain is impassable, if the value is true, the terrain is
        /// passable
        /// </summary>
        public bool[][] MapData
        {
            get
            {
                return _pixelmap;
            }
        }

        /// <summary>
        /// Returns the height of the map
        /// </summary>
        public int Height
        {
            get
            {
                return _height;
            }
        }

        /// <summary>
        /// </summary>
        public int Width
        {
            get
            {
                return _width;
            }
        }

        /// <summary>
        /// adds a specific surface along the given lines
        /// </summary>
        /// <param name="from">a point the line starts at</param>
        /// <param name="to">a point the line ends at</param>
        public void AddNewLine(Point from, Point to)
        {
            if (from.Xval > (_width - 1) || from.Xval < 0 || from.Yval > (_height - 1) || from.Yval < 0)
                throw new ArgumentException("from parameter out of range");
            if (to.Xval > (_width - 1) || to.Xval < 0 || to.Yval > (_height - 1) || to.Yval < 0)
                throw new ArgumentException("to parameter out of range");

            Line toadd = new Line(new Point(from.Xval, from.Yval), new Point(to.Xval, to.Yval));
            _surfaces.Add(toadd);
            ModifyArray(toadd);
        }

        /// <summary>
        /// adds a new line with coordinates between width and heightcompletely randomly
        /// </summary>
        /// <remarks>currently there is an edge case bug when the points are identical</remarks>
        //todo: bugfix...the "random points" are not random enough
        public void AddNewRandomLine()
        {
            //build a new random generator, grab a point, add it.
            //need to check that to != from
            Random gen = new Random();
            Point from = new Point(gen.Next(_width), gen.Next(_height));
            Point to = new Point(gen.Next(_width), gen.Next(_height));

            this.AddNewLine(from, to);
        }


        /// <summary>
        /// Modifies the internal array to note that the added line is impassable
        /// </summary>
        /// <param name="tomod">The line or surface thats impassable </param>
        /// <remarks>THis is really dangerous... Super not referentially transparent
        /// highyl reliant on hard side effects and no locking</remarks>
        private void ModifyArray(Line tomod)
        {

            if (tomod.PointA.Xval >= _width || tomod.PointA.Xval < 0 || tomod.PointA.Yval >= _height || tomod.PointA.Yval < 0)
                throw new ArgumentException("arguments Point a was out of range");
            if (tomod.PointB.Xval >= _width || tomod.PointB.Xval < 0 || tomod.PointB.Yval >= _height || tomod.PointB.Yval < 0)
                throw new ArgumentException("arguments Point b was out of range");

            //negate out all the surface area as impassable
            //get the initial point, negate it, get the slope,s
            //negate every value along the slope until the end.

            //calculate the difference in xvalues and the difference in y values
            int xdif = tomod.PointA.Xval - tomod.PointB.Xval;
            int ydif = tomod.PointA.Yval - tomod.PointB.Yval;

            //number of decrements to make
            int decs = 0;
            Point init = tomod.PointA;

            //general algorithm:
            //While not done, reduce the larger of x and y dif by an amount proportional
            //to the difference between them, then reduce the lesser by 1. This creates a
            //balancing act, so the larger is 9 and the lesser is 3, the larger will be reduced
            //by 3, and the lesser will be reduced by 1, repeat.
            //In the process "walk" along as many as the larger number is decremented by,
            //negating them.
            //Pass the new initial point into the loop.
            while (xdif > 0 && ydif > 0)
            {
                //calculate which one to reduce by many
                if (xdif > ydif)
                {
                    //number of x decrements to do
                    decs = (int)(Math.Abs(Math.Ceiling((double)(xdif / ydif))));
                    //walk the negations backwards
                    for (int i = 0; i <= decs; i++)
                    {
                        //_pixelmap[init.Xval - ((decs - i) * Math.Sign(xdif)), init.Yval] = false;
                    }
                    //pass the next point to start from in
                    init = new Point((init.Xval - decs), init.Yval - (1 * (Math.Sign(ydif))));
                    //calculate the difference in xvalues and the difference in y values
                    //needed to break out of loop
                    xdif = xdif + decs;
                    ydif = ydif - (1 * Math.Sign(xdif));
                }
                else
                {
                    //number of y decrements to do
                    decs = (int)Math.Abs(Math.Ceiling((double)(xdif / ydif)));

                    for (int i = 0; i <= decs; i++)
                    {
//                        _pixelmap[init.Xval, (init.Yval - ((decs - i) * Math.Sign(ydif)))] = false;
                    }

                    init = new Point((init.Xval - (1 * Math.Sign(xdif))), init.Yval - (decs * Math.Sign(ydif)));
                    //calculate the difference in xvalues and the difference in y values
                    //needed to break out of loop
                    xdif = xdif - (1 * Math.Sign(xdif));
                    ydif = ydif - (decs * Math.Sign(ydif));
                }
//                System.Buffer.BlockCopy(_pixelmap, 0, _serializablepixelmap, 0, _width * _height);
            }
        }
    }
}

