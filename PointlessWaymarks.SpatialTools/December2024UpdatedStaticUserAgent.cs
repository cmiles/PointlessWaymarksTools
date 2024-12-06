using Garmin.Connect.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointlessWaymarks.SpatialTools
{
    /// <summary>
    /// As of 12/4/2024 use this work around to establish a connection to Garmin Connect:
    /// https://github.com/sealbro/dotnet.garmin.connect/issues/94
    /// https://github.com/cyberjunky/python-garminconnect/commit/7c8670fc281f90183c2dcc5137cad09eab593df3
    /// https://github.com/matin/garth/commit/960d8c0ac0b68672e9edc7b9738ba77d60fa806a
    /// </summary>
    public class December2024UpdatedStaticUserAgent : IUserAgent
    {
        public string New => "GCM-iOS-5.7.2.1";
    }
}
