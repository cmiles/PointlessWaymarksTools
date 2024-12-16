using MathNet.Numerics.Interpolation;

namespace PointlessWaymarks.SpatialTools;

//This class is based on the Python code in this repository: https://github.com/ciaranbe/pyIGRF/tree/ (MIT License) largely
//because it is the basis for the code found here:
// [IAGA V-MOD Geomagnetic Field Modeling: International Geomagnetic Reference Field IGRF-13](https://www.ngdc.noaa.gov/IAGA/vmod/igrf.html)
//  NOTE: the zipped version on the link above is behind the repo and there is a critical bug fix in the repo
//   code - do not start from the zipped version. The link above has a link to C code also.
//If you are looking for alternative Python code bases see this issue and page:
//  [Too many "IGRF"s · Issue #2 · ciaranbe/pyIGRF](https://github.com/ciaranbe/pyIGRF/issues/2)
//  [pleiszenburg/pyCRGI: pyCRGI is a Python package offering the IGRF-13 (International Geomagnetic Reference Field) model.](https://github.com/pleiszenburg/pyCRGI#alternatives)
//
//It would take me considerable time to write this code from first principles - this is not a new C# implementation but
//rather just a port of the Python version. If you can find another C# version it would be worth exploring, I did
//not find one? There is C# code for the WMM model, but I wanted coverage of historic values so went with the IGRF
//model. C# WMM Code:
// [etfovac/wmm-cs: World Magnetic Model - Magnetic Declination Estimation in Point. C# Windows.Forms GUI.](https://github.com/etfovac/wmm-cs?tab=readme-ov-file)
// [Magnetic Declination in C# – Blue Toque Consulting](https://bluetoque.ca/2013/01/magnetic-declination-in-c-sharp/)
// [Tronald/CoordinateSharp: A library designed to ease geographic coordinate format conversions, and determine sun/moon information in C#](https://github.com/Tronald/CoordinateSharp) 
//
//Test values were calculate using the online calculator at:
//http://www.geomag.bgs.ac.uk/data_service/models_compass/igrf_calc.html
public static class IgrfMagneticModelTools
{
    public enum InternalShcModel
    {
        Igrf13Model,
        Igrf14Model
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    public static (double height, double beta) GeoToGg(double radius, double theta)
    {
        // Use WGS-84 ellipsoid parameters
        const double a = 6378.137; // equatorial radius
        const double b = 6356.752; // polar radius

        var a2 = a * a;
        var b2 = b * b;

        var e2 = (a2 - b2) / a2; // squared eccentricity
        var e4 = e2 * e2;
        var ep2 = (a2 - b2) / b2; // squared primed eccentricity

        var r = radius * Math.Sin(DegreesToRadians(theta));
        var z = radius * Math.Cos(DegreesToRadians(theta));

        var r2 = r * r;
        var z2 = z * z;

        var F = 54 * b2 * z2;

        var G = r2 + (1.0 - e2) * z2 - e2 * (a2 - b2);

        var c = e4 * F * r2 / Math.Pow(G, 3);

        var s = Math.Pow(1.0 + c + Math.Sqrt(c * c + 2 * c), 1.0 / 3);

        var P = F / (3 * Math.Pow(s + 1.0 / s + 1.0, 2) * G * G);

        var Q = Math.Sqrt(1.0 + 2 * e4 * P);

        var r0 = -P * e2 * r / (1.0 + Q) + Math.Sqrt(
            0.5 * a2 * (1.0 + 1.0 / Q) - P * (1.0 - e2) * z2 / (Q * (1.0 + Q)) - 0.5 * P * r2);

        var U = Math.Sqrt((r - e2 * r0) * (r - e2 * r0) + z2);

        var V = Math.Sqrt((r - e2 * r0) * (r - e2 * r0) + (1.0 - e2) * z2);

        var z0 = b2 * z / (a * V);

        var height = U * (1.0 - b2 / (a * V));

        var beta = 90.0 - RadiansToDegrees(Math.Atan2(z + ep2 * z0, r));

        return (height, beta);
    }

    public static IgrfGeomagneticData GetGeomagneticData(double lat, double lon, double altitudeInMeters,
        DateOnly date,
        InternalShcModel model = InternalShcModel.Igrf14Model)
    {
        var dayOfYear = date.DayOfYear;
        var daysInYear = DateTime.IsLeapYear(date.Year) ? 366 : 365;
        var year = date.Year + ((double)dayOfYear - 1) / daysInYear;

        var igrf = LoadInternalShcModel(model);

        //alt, colat, sd, cd = iut.gg_to_geo(alt, colat)
        var (alt, colat, sd, cd) = GgToGeo(altitudeInMeters / 1000.0, 90 - lat);

        // Create an interpolator for each coefficient
        var interpolators = new List<IInterpolation>();
        for (var i = 0; i < igrf.coeffs.GetLength(0); i++)
        {
            var coeffColumn = new double[igrf.coeffs.GetLength(1)];
            for (var j = 0; j < igrf.coeffs.GetLength(1); j++) coeffColumn[j] = igrf.coeffs[i, j];
            interpolators.Add(LinearSpline.Interpolate(igrf.time, coeffColumn));
        }

        // Interpolate the coefficients for the desired date
        var coeffs = new double[igrf.coeffs.GetLength(0)];
        for (var i = 0; i < interpolators.Count; i++) coeffs[i] = interpolators[i].Interpolate(year);

        var (Br, Bt, Bp) = SynthValues(coeffs, alt, colat, lon,
            (int)igrf.parameters["nmax"]);

        // For the SV, find the 5 year period in which the date lies and compute
        // the SV within that period. IGRF has constant SV between each 5 year period
        var epoch = (int)((year - 1900) / 5);
        var epochStart = epoch * 5;

        var coeffsSv1 = new double[igrf.coeffs.GetLength(0)];
        for (var i = 0; i < igrf.coeffs.GetLength(0); i++)
            coeffsSv1[i] = interpolators[i].Interpolate(1900 + epochStart + 1);


        var coeffsSv2 = new double[igrf.coeffs.GetLength(0)];
        for (var i = 0; i < igrf.coeffs.GetLength(0); i++)
            coeffsSv2[i] = interpolators[i].Interpolate(1900 + epochStart);


        // Add 1900 back on plus 1 year to account for SV in nT per year (nT/yr)
        var coeffsSv = new double[igrf.coeffs.GetLength(0)];
        for (var i = 0; i < igrf.coeffs.GetLength(0); i++)
            coeffsSv[i] = interpolators[i].Interpolate(1900 + epochStart + 1) -
                          interpolators[i].Interpolate(1900 + epochStart);

        var (Brs, Bts, Bps) =
            SynthValues(coeffsSv, alt, colat, lon, (int)igrf.parameters["nmax"]);

        // Use the main field coefficients from the start of each five epoch
        // to compute the SV for Dec, Inc, Hor and Total Field (F)
        var coeffsM = new double[igrf.coeffs.GetLength(0)];
        for (var i = 0; i < igrf.coeffs.GetLength(0); i++) coeffsM[i] = interpolators[i].Interpolate(1900 + epochStart);

        var (Brm, Btm, Bpm) =
            SynthValues(coeffsM, alt, colat, lon, (int)igrf.parameters["nmax"]);

        // Rearrange to X, Y, Z components
        var X = -Bt[0];
        var Y = Bp[0];
        var Z = -Br[0];

        // For the SV
        var dX = -Bts[0];
        var dY = Bps[0];
        var dZ = -Brs[0];
        var Xm = -Btm[0];
        var Ym = Bpm[0];
        var Zm = -Brm[0];

        // Rotate X and Z
        var t = X;
        X = X * cd + Z * sd;
        Z = Z * cd - t * sd;

        // Rotate dX and dZ
        t = dX;
        dX = dX * cd + dZ * sd;
        dZ = dZ * cd - t * sd;

        // Rotate Xm and Zm
        t = Xm;
        Xm = Xm * cd + Zm * sd;
        Zm = Zm * cd - t * sd;


        // Compute the four non-linear components
        var (dec, hoz, inc, eff) = Xyz2Dhif(X, Y, Z);
        // The IGRF SV coefficients are relative to the main field components
        // at the start of each five year epoch e.g. 2010, 2015, 2020
        var (decs, hozs, incs, effs) = Xyz2DhifSv(Xm, Ym, Zm, dX, dY, dZ);

        return new IgrfGeomagneticData
        {
            Date = date,
            Latitude = lat,
            Longitude = lon,
            AltitudeInMeters = altitudeInMeters,
            Declination = dec,
            Inclination = inc,
            HorizontalIntensity = hoz,
            TotalIntensity = eff,
            NorthComponent = X,
            EastComponent = Y,
            VerticalComponent = Z,
            SecularVariationDeclination = decs,
            SecularVariationInclination = incs,
            SecularVariationHorizontalIntensity = hozs,
            SecularVariationTotalIntensity = effs,
            SecularVariationNorth = dX,
            SecularVariationEast = dY,
            SecularVariationVertical = dZ
        };
    }

    public static (double radius, double theta, double sd, double cd) GgToGeo(double h, double gdcolat)
    {
        // Use WGS-84 ellipsoid parameters
        const double eqrad = 6378.137; // equatorial radius
        const double flat = 1 / 298.257223563;
        var plrad = eqrad * (1 - flat); // polar radius
        var ctgd = Math.Cos(DegreesToRadians(gdcolat));
        var stgd = Math.Sin(DegreesToRadians(gdcolat));
        var a2 = eqrad * eqrad;
        var a4 = a2 * a2;
        var b2 = plrad * plrad;
        var b4 = b2 * b2;
        var c2 = ctgd * ctgd;
        var s2 = 1 - c2;
        var rho = Math.Sqrt(a2 * s2 + b2 * c2);

        var rad = Math.Sqrt(h * (h + 2 * rho) + (a4 * s2 + b4 * c2) / (rho * rho));

        var cd = (h + rho) / rad;
        var sd = (a2 - b2) * ctgd * stgd / (rho * rad);

        var cthc = ctgd * cd - stgd * sd; // Also: sthc = stgd * cd + ctgd * sd
        var thc = RadiansToDegrees(Math.Acos(cthc)); // arccos returns values in [0, pi]

        return (rad, thc, sd, cd);
    }

    public static string Igrf13Model()
    {
        return
            """
            # IGRF 13
            # International Geomagnetic Reference Field -- 13th generation
            # see http://www.ngdc.noaa.gov/IAGA/vmod/igrf.html for detailed description
            1  13 26 2 1 1900.0 2025.0
                   1900.0 1905.0 1910.0 1915.0 1920.0 1925.0 1930.0 1935.0 1940.0 1945.0 1950.0 1955.0 1960.0 1965.0 1970.0 1975.0 1980.0 1985.0 1990.0 1995.0   2000.0    2005.0    2010.0    2015.0   2020.0   2025.0
             1   0 -31543 -31464 -31354 -31212 -31060 -30926 -30805 -30715 -30654 -30594 -30554 -30500 -30421 -30334 -30220 -30100 -29992 -29873 -29775 -29692 -29619.4 -29554.63 -29496.57 -29441.46 -29404.8 -29376.3
             1   1  -2298  -2298  -2297  -2306  -2317  -2318  -2316  -2306  -2292  -2285  -2250  -2215  -2169  -2119  -2068  -2013  -1956  -1905  -1848  -1784  -1728.2  -1669.05  -1586.42  -1501.77  -1450.9  -1413.9
             1  -1   5922   5909   5898   5875   5845   5817   5808   5812   5821   5810   5815   5820   5791   5776   5737   5675   5604   5500   5406   5306   5186.1   5077.99   4944.26   4795.99   4652.5   4523.0
             2   0   -677   -728   -769   -802   -839   -893   -951  -1018  -1106  -1244  -1341  -1440  -1555  -1662  -1781  -1902  -1997  -2072  -2131  -2200  -2267.7  -2337.24  -2396.06  -2445.88  -2499.6  -2554.6
             2   1   2905   2928   2948   2956   2959   2969   2980   2984   2981   2990   2998   3003   3002   2997   3000   3010   3027   3044   3059   3070   3068.4   3047.69   3026.34   3012.20   2982.0   2947.0
             2  -1  -1061  -1086  -1128  -1191  -1259  -1334  -1424  -1520  -1614  -1702  -1810  -1898  -1967  -2016  -2047  -2067  -2129  -2197  -2279  -2366  -2481.6  -2594.50  -2708.54  -2845.41  -2991.6  -3142.6
             2   2    924   1041   1176   1309   1407   1471   1517   1550   1566   1578   1576   1581   1590   1594   1611   1632   1663   1687   1686   1681   1670.9   1657.76   1668.17   1676.35   1677.0   1666.5
             2  -2   1121   1065   1000    917    823    728    644    586    528    477    381    291    206    114     25    -68   -200   -306   -373   -413   -458.0   -515.43   -575.73   -642.17   -734.6   -846.6
             3   0   1022   1037   1058   1084   1111   1140   1172   1206   1240   1282   1297   1302   1302   1297   1287   1276   1281   1296   1314   1335   1339.6   1336.30   1339.85   1350.33   1363.2   1374.2
             3   1  -1469  -1494  -1524  -1559  -1600  -1645  -1692  -1740  -1790  -1834  -1889  -1944  -1992  -2038  -2091  -2144  -2180  -2208  -2239  -2267  -2288.0  -2305.83  -2326.54  -2352.26  -2381.2  -2410.7
             3  -1   -330   -357   -389   -421   -445   -462   -480   -494   -499   -499   -476   -462   -414   -404   -366   -333   -336   -310   -284   -262   -227.6   -198.86   -160.40   -115.29    -82.1    -52.1
             3   2   1256   1239   1223   1212   1205   1202   1205   1215   1232   1255   1274   1288   1289   1292   1278   1260   1251   1247   1248   1249   1252.1   1246.39   1232.10   1225.85   1236.2   1251.7
             3  -2      3     34     62     84    103    119    133    146    163    186    206    216    224    240    251    262    271    284    293    302    293.4    269.72    251.75    245.04    241.9    236.4
             3   3    572    635    705    778    839    881    907    918    916    913    896    882    878    856    838    830    833    829    802    759    714.5    672.51    633.73    581.69    525.7    465.7
             3  -3    523    480    425    360    293    229    166    101     43    -11    -46    -83   -130   -165   -196   -223   -252   -297   -352   -427   -491.1   -524.72   -537.03   -538.70   -543.4   -540.9
             4   0    876    880    884    887    889    891    896    903    914    944    954    958    957    957    952    946    938    936    939    940    932.3    920.55    912.66    907.42    903.0    897.0
             4   1    628    643    660    678    695    711    727    744    762    776    792    796    800    804    800    791    782    780    780    780    786.8    797.96    808.97    813.68    809.5    801.5
             4  -1    195    203    211    218    220    216    205    188    169    144    136    133    135    148    167    191    212    232    247    262    272.6    282.07    286.48    283.54    281.9    281.4
             4   2    660    653    644    631    616    601    584    565    550    544    528    510    504    479    461    438    398    361    325    290    250.0    210.65    166.58    120.49     86.3     56.8
             4  -2    -69    -77    -90   -109   -134   -163   -195   -226   -252   -276   -278   -274   -278   -269   -266   -265   -257   -249   -240   -236   -231.9   -225.23   -211.03   -188.43   -158.4   -125.9
             4   3   -361   -380   -400   -416   -424   -426   -422   -415   -405   -421   -408   -397   -394   -390   -395   -405   -419   -424   -423   -418   -403.0   -379.86   -356.83   -334.85   -309.4   -283.4
             4  -3   -210   -201   -189   -173   -153   -130   -109    -90    -72    -55    -37    -23      3     13     26     39     53     69     84     97    119.8    145.15    164.46    180.95    199.7    217.7
             4   4    134    146    160    178    199    217    234    249    265    304    303    290    269    252    234    216    199    170    141    122    111.3    100.00     89.40     70.38     48.0     22.5
             4  -4    -75    -65    -55    -51    -57    -70    -90   -114   -141   -178   -210   -230   -255   -269   -279   -288   -297   -297   -299   -306   -303.8   -305.36   -309.72   -329.23   -349.7   -374.7
             5   0   -184   -192   -201   -211   -221   -230   -237   -241   -241   -253   -240   -229   -222   -219   -216   -218   -218   -214   -214   -214   -218.8   -227.00   -230.87   -232.91   -234.3   -235.8
             5   1    328    328    327    327    326    326    327    329    334    346    349    360    362    358    359    356    357    355    353    352    351.4    354.41    357.29    360.14    363.2    365.7
             5  -1   -210   -193   -172   -148   -122    -96    -72    -51    -33    -12      3     15     16     19     26     31     46     47     46     46     43.8     42.72     44.58     46.98     47.7     47.7
             5   2    264    259    253    245    236    226    218    211    208    194    211    230    242    254    262    264    261    253    245    235    222.3    208.95    200.26    192.35    187.8    184.8
             5  -2     53     56     57     58     58     58     60     64     71     95    103    110    125    128    139    148    150    150    154    165    171.9    180.25    189.01    196.98    208.3    220.8
             5   3      5     -1     -9    -16    -23    -28    -32    -33    -33    -20    -20    -23    -26    -31    -42    -59    -74    -93   -109   -118   -130.4   -136.54   -141.05   -140.94   -140.7   -139.7
             5  -3    -33    -32    -33    -34    -38    -44    -53    -64    -75    -67    -87    -98   -117   -126   -139   -152   -151   -154   -153   -143   -133.1   -123.45   -118.06   -119.14   -121.2   -124.2
             5   4    -86    -93   -102   -111   -119   -125   -131   -136   -141   -142   -147   -152   -156   -157   -160   -159   -162   -164   -165   -166   -168.6   -168.05   -163.17   -157.40   -151.2   -144.7
             5  -4   -124   -125   -126   -126   -125   -122   -118   -115   -113   -119   -122   -121   -114    -97    -91    -83    -78    -75    -69    -55    -39.3    -19.57     -0.01     15.98     32.3     47.3
             5   5    -16    -26    -38    -51    -62    -69    -74    -76    -76    -82    -76    -69    -63    -62    -56    -49    -48    -46    -36    -17    -12.9    -13.55     -8.03      4.30     13.5     18.0
             5  -5      3     11     21     32     43     51     58     64     69     82     80     78     81     81     83     88     92     95     97    107    106.3    103.85    101.04    100.12     98.9    100.4
             6   0     63     62     62     61     61     61     60     59     57     59     54     47     46     45     43     45     48     53     61     68     72.3     73.60     72.78     69.55     66.0     63.5
             6   1     61     60     58     57     55     54     53     53     54     57     57     57     58     61     64     66     66     65     65     67     68.2     69.56     68.69     67.57     65.5     64.0
             6  -1     -9     -7     -5     -2      0      3      4      4      4      6     -1     -9    -10    -11    -12    -13    -15    -16    -16    -17    -17.4    -20.33    -20.90    -20.61    -19.1    -19.1
             6   2    -11    -11    -11    -10    -10     -9     -9     -8     -7      6      4      3      1      8     15     28     42     51     59     68     74.2     76.74     75.92     72.79     72.9     74.9
             6  -2     83     86     89     93     96     99    102    104    105    100     99     96     99    100    100     99     93     88     82     72     63.7     54.75     44.18     33.30     25.1     17.1
             6   3   -217   -221   -224   -228   -233   -238   -242   -246   -249   -246   -247   -247   -237   -228   -212   -198   -192   -185   -178   -170   -160.9   -151.34   -141.40   -129.85   -121.5   -115.0
             6  -3      2      4      5      8     11     14     19     25     33     16     33     48     60     68     72     75     71     69     69     67     65.1     63.63     61.54     58.74     52.8     46.3
             6   4    -58    -57    -54    -51    -46    -40    -32    -25    -18    -25    -16     -8     -1      4      2      1      4      4      3     -1     -5.9    -14.58    -22.83    -28.93    -36.2    -43.2
             6  -4    -35    -32    -29    -26    -22    -18    -16    -15    -15     -9    -12    -16    -20    -32    -37    -41    -43    -48    -52    -58    -61.2    -63.53    -66.26    -66.64    -64.5    -60.5
             6   5     59     57     54     49     44     39     32     25     18     21     12      7     -2      1      3      6     14     16     18     19     16.9     14.58     13.10     13.14     13.5     13.5
             6  -5     36     32     28     23     18     13      8      4      0    -16    -12    -12    -11     -8     -6     -4     -2     -1      1      1      0.7      0.24      3.02      7.35      8.9      8.9
             6   6    -90    -92    -95    -98   -101   -103   -104   -106   -107   -104   -105   -107   -113   -111   -112   -111   -108   -102    -96    -93    -90.4    -86.36    -78.09    -70.85    -64.7    -60.2
             6  -6    -69    -67    -65    -62    -57    -52    -46    -40    -33    -39    -30    -24    -17     -7      1     11     17     21     24     36     43.8     50.94     55.40     62.41     68.1     73.1
             7   0     70     70     71     72     73     73     74     74     74     70     65     65     67     75     72     71     72     74     77     77     79.0     79.88     80.44     81.29     80.6     80.1
             7   1    -55    -54    -54    -54    -54    -54    -54    -53    -53    -40    -55    -56    -56    -57    -57    -56    -59    -62    -64    -72    -74.0    -74.46    -75.00    -75.99    -76.7    -77.7
             7  -1    -45    -46    -47    -48    -49    -50    -51    -52    -52    -45    -35    -50    -55    -61    -70    -77    -82    -83    -80    -69    -64.6    -61.14    -57.80    -54.27    -51.5    -48.5
             7   2      0      0      1      2      2      3      4      4      4      0      2      2      5      4      1      1      2      3      2      1      0.0     -1.65     -4.55     -6.79     -8.2     -8.2
             7  -2    -13    -14    -14    -14    -14    -14    -15    -17    -18    -18    -17    -24    -28    -27    -27    -26    -27    -27    -26    -25    -24.2    -22.57    -21.20    -19.53    -16.9    -13.9
             7   3     34     33     32     31     29     27     25     23     20      0      1     10     15     13     14     16     21     24     26     28     33.3     38.73     45.24     51.82     56.5     60.0
             7  -3    -10    -11    -12    -12    -13    -14    -14    -14    -14      2      0     -4     -6     -2     -4     -5     -5     -2      0      4      6.2      6.82      6.54      5.59      2.2     -1.8
             7   4    -41    -41    -40    -38    -37    -35    -34    -33    -31    -29    -40    -32    -32    -26    -22    -14    -12     -6     -1      5      9.1     12.30     14.00     15.07     15.8     16.3
             7  -4     -1      0      1      2      4      5      6      7      7      6     10      8      7      6      8     10     16     20     21     24     24.0     25.35     24.96     24.45     23.5     22.5
             7   5    -21    -20    -19    -18    -16    -14    -12    -11     -9    -10     -7    -11     -7     -6     -2      0      1      4      5      4      6.9      9.37     10.46      9.32      6.4      3.9
             7  -5     28     28     28     28     28     29     29     29     29     28     36     28     23     26     23     22     18     17     17     17     14.8     10.93      7.03      3.27     -2.2     -7.7
             7   6     18     18     18     19     19     19     18     18     17     15      5      9     17     13     13     12     11     10      9      8      7.3      5.42      1.64     -2.88     -7.2    -11.2
             7  -6    -12    -12    -13    -15    -16    -17    -18    -19    -20    -17    -18    -20    -18    -23    -23    -23    -23    -23    -23    -24    -25.4    -26.32    -27.61    -27.50    -27.2    -26.7
             7   7      6      6      6      6      6      6      6      6      5     29     19     18      8      1     -2     -5     -2      0      0     -2     -1.2      1.94      4.92      6.61      9.8     13.8
             7  -7    -22    -22    -22    -22    -22    -21    -20    -19    -19    -22    -16    -18    -17    -12    -11    -12    -10     -7     -4     -6     -5.8     -4.64     -3.28     -2.32     -1.8     -0.3
             8   0     11     11     11     11     11     11     11     11     11     13     22     11     15     13     14     14     18     21     23     25     24.4     24.80     24.41     23.98     23.7     23.7
             8   1      8      8      8      8      7      7      7      7      7      7     15      9      6      5      6      6      6      6      5      6      6.6      7.62      8.21      8.89      9.7     10.2
             8  -1      8      8      8      8      8      8      8      8      8     12      5     10     11      7      7      6      7      8     10     11     11.9     11.20     10.84     10.04      8.4      7.4
             8   2     -4     -4     -4     -4     -3     -3     -3     -3     -3     -8     -4     -6     -4     -4     -2     -1      0      0     -1     -6     -9.2    -11.73    -14.50    -16.78    -17.6    -18.1
             8  -2    -14    -15    -15    -15    -15    -15    -15    -15    -14    -21    -22    -15    -14    -12    -15    -16    -18    -19    -19    -21    -21.5    -20.88    -20.03    -18.26    -15.3    -12.3
             8   3     -9     -9     -9     -9     -9     -9     -9     -9    -10     -5     -1    -14    -11    -14    -13    -12    -11    -11    -10     -9     -7.9     -6.88     -5.59     -3.16     -0.5      1.5
             8  -3      7      7      6      6      6      6      5      5      5    -12      0      5      7      9      6      4      4      5      6      8      8.5      9.83     11.83     13.18     12.8     11.8
             8   4      1      1      1      2      2      2      2      1      1      9     11      6      2      0     -3     -8     -7     -9    -12    -14    -16.6    -18.11    -19.34    -20.56    -21.1    -21.6
             8  -4    -13    -13    -13    -13    -14    -14    -14    -15    -15     -7    -21    -23    -18    -16    -17    -19    -22    -23    -22    -23    -21.5    -19.71    -17.41    -14.60    -11.7     -9.2
             8   5      2      2      2      3      4      4      5      6      6      7     15     10     10      8      5      4      4      4      3      9      9.1     10.17     11.61     13.33     15.3     17.3
             8  -5      5      5      5      5      5      5      5      5      5      2     -8      3      4      4      6      6      9     11     12     15     15.5     16.22     16.71     16.16     14.9     13.4
             8   6     -9     -8     -8     -8     -7     -7     -6     -6     -5    -10    -13     -7     -5     -1      0      0      3      4      4      6      7.0      9.36     10.85     11.76     13.7     15.2
             8  -6     16     16     16     16     17     17     18     18     19     18     17     23     23     24     21     18     16     14     12     11      8.9      7.61      6.96      5.69      3.6      1.6
             8   7      5      5      5      6      6      7      8      8      9      7      5      6     10     11     11     10      6      4      2     -5     -7.9    -11.25    -14.05    -15.98    -16.5    -17.0
             8  -7     -5     -5     -5     -5     -5     -5     -5     -5     -5      3     -4     -4      1     -3     -6    -10    -13    -15    -16    -16    -14.9    -12.76    -10.74     -9.10     -6.9     -4.4
             8   8      8      8      8      8      8      8      8      7      7      2     -1      9      8      4      3      1     -1     -4     -6     -7     -7.0     -4.87     -3.54     -2.02     -0.3      1.7
             8  -8    -18    -18    -18    -18    -19    -19    -19    -19    -19    -11    -17    -13    -20    -17    -16    -17    -15    -11    -10     -4     -2.1     -0.06      1.64      2.26      2.8      2.8
             9   0      8      8      8      8      8      8      8      8      8      5      3      4      4      8      8      7      5      5      4      4      5.0      5.58      5.50      5.33      5.0      5.0
             9   1     10     10     10     10     10     10     10     10     10    -21     -7      9      6     10     10     10     10     10      9      9      9.4      9.76      9.45      8.83      8.4      8.4
             9  -1    -20    -20    -20    -20    -20    -20    -20    -20    -21    -27    -24    -11    -18    -22    -21    -21    -21    -21    -20    -20    -19.7    -20.11    -20.54    -21.77    -23.4    -23.4
             9   2      1      1      1      1      1      1      1      1      1      1     -1     -4      0      2      2      2      1      1      1      3      3.0      3.58      3.45      3.02      2.9      2.9
             9  -2     14     14     14     14     14     14     14     15     15     17     19     12     12     15     16     16     16     15     15     15     13.4     12.69     11.51     10.76     11.0     11.0
             9   3    -11    -11    -11    -11    -11    -11    -12    -12    -12    -11    -25     -5     -9    -13    -12    -12    -12    -12    -12    -10     -8.4     -6.94     -5.27     -3.22     -1.5     -1.5
             9  -3      5      5      5      5      5      5      5      5      5     29     12      7      2      7      6      7      9      9     11     12     12.5     12.67     12.75     11.74      9.8      9.8
             9   4     12     12     12     12     12     12     12     11     11      3     10      2      1     10     10     10      9      9      9      8      6.3      5.01      3.13      0.67     -1.1     -1.1
             9  -4     -3     -3     -3     -3     -3     -3     -3     -3     -3     -9      2      6      0     -4     -4     -4     -5     -6     -7     -6     -6.2     -6.72     -7.14     -6.74     -5.1     -5.1
             9   5      1      1      1      1      1      1      1      1      1     16      5      4      4     -1     -1     -1     -3     -3     -4     -8     -8.9    -10.76    -12.38    -13.20    -13.2    -13.2
             9  -5     -2     -2     -2     -2     -2     -2     -2     -3     -3      4      2     -2     -3     -5     -5     -5     -6     -6     -7     -8     -8.4     -8.16     -7.42     -6.88     -6.3     -6.3
             9   6     -2     -2     -2     -2     -2     -2     -2     -2     -2     -3     -5      1     -1     -1      0     -1     -1     -1     -2     -1     -1.5     -1.25     -0.76     -0.10      1.1      1.1
             9  -6      8      8      8      8      9      9      9      9      9      9      8     10      9     10     10     10      9      9      9      8      8.4      8.10      7.97      7.79      7.8      7.8
             9   7      2      2      2      2      2      2      3      3      3     -4     -2      2     -2      5      3      4      7      7      7     10      9.3      8.76      8.43      8.68      8.8      8.8
             9  -7     10     10     10     10     10     10     10     11     11      6      8      7      8     10     11     11     10      9      8      5      3.8      2.92      2.14      1.04      0.4      0.4
             9   8     -1      0      0      0      0      0      0      0      1     -3      3      2      3      1      1      1      2      1      1     -2     -4.3     -6.66     -8.42     -9.06     -9.3     -9.3
             9  -8     -2     -2     -2     -2     -2     -2     -2     -2     -2      1    -11     -6      0     -4     -2     -3     -6     -7     -7     -8     -8.2     -7.73     -6.08     -3.89     -1.4     -1.4
             9   9     -1     -1     -1     -1     -1     -1     -2     -2     -2     -4      8      5     -1     -2     -1     -2     -5     -5     -6     -8     -8.2     -9.22    -10.08    -10.54    -11.9    -11.9
             9  -9      2      2      2      2      2      2      2      2      2      8     -7      5      5      1      1      1      2      2      2      3      4.8      6.01      7.01      8.44      9.6      9.6
            10   0     -3     -3     -3     -3     -3     -3     -3     -3     -3     -3     -8     -3      1     -2     -3     -3     -4     -4     -3     -3     -2.6     -2.17     -1.94     -2.01     -1.9     -1.9
            10   1     -4     -4     -4     -4     -4     -4     -4     -4     -4     11      4     -5     -3     -3     -3     -3     -4     -4     -4     -6     -6.0     -6.12     -6.24     -6.26     -6.2     -6.2
            10  -1      2      2      2      2      2      2      2      2      2      5     13     -4      4      2      1      1      1      1      2      1      1.7      2.19      2.73      3.28      3.4      3.4
            10   2      2      2      2      2      2      2      2      2      2      1     -1     -1      4      2      2      2      2      3      2      2      1.7      1.42      0.89      0.17     -0.1     -0.1
            10  -2      1      1      1      1      1      1      1      1      1      1     -2      0      1      1      1      1      0      0      1      0      0.0      0.10     -0.10     -0.40     -0.2     -0.2
            10   3     -5     -5     -5     -5     -5     -5     -5     -5     -5      2     13      2      0     -5     -5     -5     -5     -5     -5     -4     -3.1     -2.35     -1.07      0.55      1.7      1.7
            10  -3      2      2      2      2      2      2      2      2      2    -20    -10     -8      0      2      3      3      3      3      3      4      4.0      4.46      4.71      4.55      3.6      3.6
            10   4     -2     -2     -2     -2     -2     -2     -2     -2     -2     -5     -4     -3     -1     -2     -1     -2     -2     -2     -2     -1     -0.5     -0.15     -0.16     -0.55     -0.9     -0.9
            10  -4      6      6      6      6      6      6      6      6      6     -1      2     -2      2      6      4      4      6      6      6      5      4.9      4.76      4.44      4.40      4.8      4.8
            10   5      6      6      6      6      6      6      6      6      6     -1      4      7      4      4      6      5      5      5      4      4      3.7      3.06      2.45      1.70      0.7      0.7
            10  -5     -4     -4     -4     -4     -4     -4     -4     -4     -4     -6     -3     -4     -5     -4     -4     -4     -4     -4     -4     -5     -5.9     -6.58     -7.22     -7.92     -8.6     -8.6
            10   6      4      4      4      4      4      4      4      4      4      8     12      4      6      4      4      4      3      3      3      2      1.0      0.29     -0.33     -0.67     -0.9     -0.9
            10  -6      0      0      0      0      0      0      0      0      0      6      6      1      1      0      0     -1      0      0      0     -1     -1.2     -1.01     -0.96     -0.61     -0.1     -0.1
            10   7      0      0      0      0      0      0      0      0      0     -1      3     -2      1      0      1      1      1      1      1      2      2.0      2.06      2.13      2.13      1.9      1.9
            10  -7     -2     -2     -2     -2     -2     -2     -2     -1     -1     -4     -3     -3     -1     -2     -1     -1     -1     -1     -2     -2     -2.9     -3.47     -3.95     -4.16     -4.3     -4.3
            10   8      2      2      2      1      1      1      1      2      2     -3      2      6     -1      2      0      0      2      2      3      5      4.2      3.77      3.09      2.33      1.4      1.4
            10  -8      4      4      4      4      4      4      4      4      4     -2      6      7      6      3      3      3      4      4      3      1      0.2     -0.86     -1.99     -2.85     -3.4     -3.4
            10   9      2      2      2      2      3      3      3      3      3      5     10     -2      2      2      3      3      3      3      3      1      0.3     -0.21     -1.03     -1.80     -2.4     -2.4
            10  -9      0      0      0      0      0      0      0      0      0      0     11     -1      0      0      1      1      0      0     -1     -2     -2.2     -2.31     -1.97     -1.12     -0.1     -0.1
            10  10      0      0      0      0      0      0      0      0      0     -2      3      0      0      0     -1     -1      0      0      0      0     -1.1     -2.09     -2.80     -3.59     -3.8     -3.8
            10 -10     -6     -6     -6     -6     -6     -6     -6     -6     -6     -2      8     -3     -7     -6     -4     -5     -6     -6     -6     -7     -7.4     -7.93     -8.31     -8.72     -8.8     -8.8
            11   0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      2.7      2.95      3.05      3.00      3.0      3.0
            11   1      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -1.7     -1.60     -1.48     -1.40     -1.4     -1.4
            11  -1      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.1      0.26      0.13      0.00      0.0      0.0
            11   2      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -1.9     -1.88     -2.03     -2.30     -2.5     -2.5
            11  -2      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      1.3      1.44      1.67      2.11      2.5      2.5
            11   3      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      1.5      1.44      1.65      2.08      2.3      2.3
            11  -3      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.9     -0.77     -0.66     -0.60     -0.6     -0.6
            11   4      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.1     -0.31     -0.51     -0.79     -0.9     -0.9
            11  -4      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -2.6     -2.27     -1.76     -1.05     -0.4     -0.4
            11   5      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.1      0.29      0.54      0.58      0.3      0.3
            11  -5      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.9      0.90      0.85      0.76      0.6      0.6
            11   6      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.7     -0.79     -0.79     -0.70     -0.7     -0.7
            11  -6      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.7     -0.58     -0.39     -0.20     -0.2     -0.2
            11   7      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.7      0.53      0.37      0.14     -0.1     -0.1
            11  -7      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -2.8     -2.69     -2.51     -2.12     -1.7     -1.7
            11   8      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      1.7      1.80      1.79      1.70      1.4      1.4
            11  -8      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.9     -1.08     -1.27     -1.44     -1.6     -1.6
            11   9      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.1      0.16      0.12     -0.22     -0.6     -0.6
            11  -9      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -1.2     -1.58     -2.11     -2.57     -3.0     -3.0
            11  10      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      1.2      0.96      0.75      0.44      0.2      0.2
            11 -10      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -1.9     -1.90     -1.94     -2.01     -2.0     -2.0
            11  11      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      4.0      3.99      3.75      3.49      3.1      3.1
            11 -11      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.9     -1.39     -1.86     -2.34     -2.6     -2.6
            12   0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -2.2     -2.15     -2.12     -2.09     -2.0     -2.0
            12   1      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.3     -0.29     -0.21     -0.16     -0.1     -0.1
            12  -1      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.55     -0.87     -1.08     -1.2     -1.2
            12   2      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.2      0.21      0.30      0.46      0.5      0.5
            12  -2      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.23      0.27      0.37      0.5      0.5
            12   3      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.9      0.89      1.04      1.23      1.3      1.3
            12  -3      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      2.5      2.38      2.13      1.75      1.4      1.4
            12   4      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.2     -0.38     -0.63     -0.89     -1.2     -1.2
            12  -4      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -2.6     -2.63     -2.49     -2.19     -1.8     -1.8
            12   5      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.9      0.96      0.95      0.85      0.7      0.7
            12  -5      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.7      0.61      0.49      0.27      0.1      0.1
            12   6      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.5     -0.30     -0.11      0.10      0.3      0.3
            12  -6      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.40      0.59      0.72      0.8      0.8
            12   7      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.46      0.52      0.54      0.5      0.5
            12  -7      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.0      0.01      0.00     -0.09     -0.2     -0.2
            12   8      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.3     -0.35     -0.39     -0.37     -0.3     -0.3
            12  -8      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.0      0.02      0.13      0.29      0.6      0.6
            12   9      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.36     -0.37     -0.43     -0.5     -0.5
            12  -9      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.28      0.27      0.23      0.2      0.2
            12  10      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.1      0.08      0.21      0.22      0.1      0.1
            12 -10      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.9     -0.87     -0.86     -0.89     -0.9     -0.9
            12  11      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.2     -0.49     -0.77     -0.94     -1.1     -1.1
            12 -11      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.34     -0.23     -0.16      0.0      0.0
            12  12      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.08      0.04     -0.03     -0.3     -0.3
            12 -12      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.8      0.88      0.87      0.72      0.5      0.5
            13   0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.2     -0.16     -0.09     -0.02      0.1      0.1
            13   1      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.9     -0.88     -0.89     -0.92     -0.9     -0.9
            13  -1      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.9     -0.76     -0.87     -0.88     -0.9     -0.9
            13   2      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.30      0.31      0.42      0.5      0.5
            13  -2      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.2      0.33      0.30      0.49      0.6      0.6
            13   3      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.1      0.28      0.42      0.63      0.7      0.7
            13  -3      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      1.8      1.72      1.66      1.56      1.4      1.4
            13   4      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.43     -0.45     -0.42     -0.3     -0.3
            13  -4      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.54     -0.59     -0.50     -0.4     -0.4
            13   5      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      1.3      1.18      1.08      0.96      0.8      0.8
            13  -5      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -1.0     -1.07     -1.14     -1.24     -1.3     -1.3
            13   6      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.37     -0.31     -0.19      0.0      0.0
            13  -6      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.1     -0.04     -0.07     -0.10     -0.1     -0.1
            13   7      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.7      0.75      0.78      0.81      0.8      0.8
            13  -7      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.7      0.63      0.54      0.42      0.3      0.3
            13   8      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.4     -0.26     -0.18     -0.13      0.0      0.0
            13  -8      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.21      0.10     -0.04     -0.1     -0.1
            13   9      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.35      0.38      0.38      0.4      0.4
            13  -9      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.6      0.53      0.49      0.48      0.5      0.5
            13  10      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.1     -0.05      0.02      0.08      0.1      0.1
            13 -10      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.3      0.38      0.44      0.48      0.5      0.5
            13  11      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.4      0.41      0.42      0.46      0.5      0.5
            13 -11      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.2     -0.22     -0.25     -0.30     -0.4     -0.4
            13  12      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.0     -0.10     -0.26     -0.35     -0.5     -0.5
            13 -12      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.5     -0.57     -0.53     -0.43     -0.4     -0.4
            13  13      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0.1     -0.18     -0.26     -0.36     -0.4     -0.4
            13 -13      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0      0     -0.9     -0.82     -0.79     -0.71     -0.6     -0.6

            """;
    }

    public static string Igrf14Model()
    {
        return
            """
            # 14th Generation International Geomagnetic Reference Field Schmidt semi-normalised spherical harmonic coefficients  degree n=1 13; released 2025                         
            # in units nanoTesla for IGRF and definitive DGRF main-field models (degree n=1 8 nanoTesla/year for secular variation (SV)) 
            1 13 27 2 1 1900.0 2030.0
              1900.0 1905.0 1910.0 1915.0 1920.0 1925.0 1930.0 1935.0 1940.0 1945.0 1950.0 1955.0 1960.0 1965.0 1970.0 1975.0 1980.0 1985.0 1990.0 1995.0 2000.0 2005.0 2010.0 2015.0 2020.0 2025.0 2030.0
            1 0 -31543 -31464 -31354 -31212 -31060 -30926 -30805 -30715 -30654 -30594 -30554 -30500 -30421 -30334 -30220 -30100 -29992 -29873 -29775 -29692 -29619.4 -29554.63 -29496.57 -29441.46 -29403.41 -29350.0 -29287.0
            1 1 -2298 -2298 -2297 -2306 -2317 -2318 -2316 -2306 -2292 -2285 -2250 -2215 -2169 -2119 -2068 -2013 -1956 -1905 -1848 -1784 -1728.2 -1669.05 -1586.42 -1501.77 -1451.37 -1410.3 -1360.3
            1 1 5922 5909 5898 5875 5845 5817 5808 5812 5821 5810 5815 5820 5791 5776 5737 5675 5604 5500 5406 5306 5186.1 5077.99 4944.26 4795.99 4653.35 4545.5 4438.0
            2 0 -677 -728 -769 -802 -839 -893 -951 -1018 -1106 -1244 -1341 -1440 -1555 -1662 -1781 -1902 -1997 -2072 -2131 -2200 -2267.7 -2337.24 -2396.06 -2445.88 -2499.78 -2556.2 -2612.2
            2 1 2905 2928 2948 2956 2959 2969 2980 2984 2981 2990 2998 3003 3002 2997 3000 3010 3027 3044 3059 3070 3068.4 3047.69 3026.34 3012.2 2981.96 2950.9 2924.4
            2 1 -1061 -1086 -1128 -1191 -1259 -1334 -1424 -1520 -1614 -1702 -1810 -1898 -1967 -2016 -2047 -2067 -2129 -2197 -2279 -2366 -2481.6 -2594.50 -2708.54 -2845.41 -2991.72 -3133.6 -3270.1
            2 2 924 1041 1176 1309 1407 1471 1517 1550 1566 1578 1576 1581 1590 1594 1611 1632 1663 1687 1686 1681 1670.9 1657.76 1668.17 1676.35 1676.85 1648.7 1607.2
            2 2 1121 1065 1000 917 823 728 644 586 528 477 381 291 206 114 25 -68 -200 -306 -373 -413 -458.0 -515.43 -575.73 -642.17 -734.62 -814.2 -869.7
            3 0 1022 1037 1058 1084 1111 1140 1172 1206 1240 1282 1297 1302 1302 1297 1287 1276 1281 1296 1314 1335 1339.6 1336.30 1339.85 1350.33 1363.00 1360.9 1353.4
            3 1 -1469 -1494 -1524 -1559 -1600 -1645 -1692 -1740 -1790 -1834 -1889 -1944 -1992 -2038 -2091 -2144 -2180 -2208 -2239 -2267 -2288.0 -2305.83 -2326.54 -2352.26 -2380.80 -2404.2 -2426.2
            3 1 -330 -357 -389 -421 -445 -462 -480 -494 -499 -499 -476 -462 -414 -404 -366 -333 -336 -310 -284 -262 -227.6 -198.86 -160.40 -115.29 -81.96 -56.9 -37.9
            3 2 1256 1239 1223 1212 1205 1202 1205 1215 1232 1255 1274 1288 1289 1292 1278 1260 1251 1247 1248 1249 1252.1 1246.39 1232.10 1225.85 1236.06 1243.8 1245.8
            3 2 3 34 62 84 103 119 133 146 163 186 206 216 224 240 251 262 271 284 293 302 293.4 269.72 251.75 245.04 241.80 237.6 236.6
            3 3 572 635 705 778 839 881 907 918 916 913 896 882 878 856 838 830 833 829 802 759 714.5 672.51 633.73 581.69 525.60 453.4 375.4
            3 3 523 480 425 360 293 229 166 101 43 -11 -46 -83 -130 -165 -196 -223 -252 -297 -352 -427 -491.1 -524.72 -537.03 -538.7 -542.52 -549.6 -569.1
            4 0 876 880 884 887 889 891 896 903 914 944 954 958 957 957 952 946 938 936 939 940 932.3 920.55 912.66 907.42 902.82 894.7 886.2
            4 1 628 643 660 678 695 711 727 744 762 776 792 796 800 804 800 791 782 780 780 780 786.8 797.96 808.97 813.68 809.47 799.6 788.1
            4 1 195 203 211 218 220 216 205 188 169 144 136 133 135 148 167 191 212 232 247 262 272.6 282.07 286.48 283.54 282.10 278.6 272.1
            4 2 660 653 644 631 616 601 584 565 550 544 528 510 504 479 461 438 398 361 325 290 250.0 210.65 166.58 120.49 86.18 55.8 26.8
            4 2 -69 -77 -90 -109 -134 -163 -195 -226 -252 -276 -278 -274 -278 -269 -266 -265 -257 -249 -240 -236 -231.9 -225.23 -211.03 -188.43 -158.50 -134.0 -113.5
            4 3 -361 -380 -400 -416 -424 -426 -422 -415 -405 -421 -408 -397 -394 -390 -395 -405 -419 -424 -423 -418 -403.0 -379.86 -356.83 -334.85 -309.47 -281.1 -254.1
            4 3 -210 -201 -189 -173 -153 -130 -109 -90 -72 -55 -37 -23 3 13 26 39 53 69 84 97 119.8 145.15 164.46 180.95 199.75 212.0 220.0
            4 4 134 146 160 178 199 217 234 249 265 304 303 290 269 252 234 216 199 170 141 122 111.3 100.00 89.40 70.38 47.44 12.0 -22.0
            4 4 -75 -65 -55 -51 -57 -70 -90 -114 -141 -178 -210 -230 -255 -269 -279 -288 -297 -297 -299 -306 -303.8 -305.36 -309.72 -329.23 -350.30 -375.4 -395.9
            5 0 -184 -192 -201 -211 -221 -230 -237 -241 -241 -253 -240 -229 -222 -219 -216 -218 -218 -214 -214 -214 -218.8 -227.00 -230.87 -232.91 -234.42 -232.9 -229.9
            5 1 328 328 327 327 326 326 327 329 334 346 349 360 362 358 359 356 357 355 353 352 351.4 354.41 357.29 360.14 363.26 369.0 375.5
            5 1 -210 -193 -172 -148 -122 -96 -72 -51 -33 -12 3 15 16 19 26 31 46 47 46 46 43.8 42.72 44.58 46.98 47.52 45.3 42.8
            5 2 264 259 253 245 236 226 218 211 208 194 211 230 242 254 262 264 261 253 245 235 222.3 208.95 200.26 192.35 187.86 187.2 187.2
            5 2 53 56 57 58 58 58 60 64 71 95 103 110 125 128 139 148 150 150 154 165 171.9 180.25 189.01 196.98 208.36 220.0 230.5
            5 3 5 -1 -9 -16 -23 -28 -32 -33 -33 -20 -20 -23 -26 -31 -42 -59 -74 -93 -109 -118 -130.4 -136.54 -141.05 -140.94 -140.73 -138.7 -135.2
            5 3 -33 -32 -33 -34 -38 -44 -53 -64 -75 -67 -87 -98 -117 -126 -139 -152 -151 -154 -153 -143 -133.1 -123.45 -118.06 -119.14 -121.43 -122.9 -120.4
            5 4 -86 -93 -102 -111 -119 -125 -131 -136 -141 -142 -147 -152 -156 -157 -160 -159 -162 -164 -165 -166 -168.6 -168.05 -163.17 -157.4 -151.16 -141.9 -130.4
            5 4 -124 -125 -126 -126 -125 -122 -118 -115 -113 -119 -122 -121 -114 -97 -91 -83 -78 -75 -69 -55 -39.3 -19.57 -0.01 15.98 32.09 42.9 51.4
            5 5 -16 -26 -38 -51 -62 -69 -74 -76 -76 -82 -76 -69 -63 -62 -56 -49 -48 -46 -36 -17 -12.9 -13.55 -8.03 4.3 13.98 20.9 25.9
            5 5 3 11 21 32 43 51 58 64 69 82 80 78 81 81 83 88 92 95 97 107 106.3 103.85 101.04 100.12 99.14 106.2 115.7
            6 0 63 62 62 61 61 61 60 59 57 59 54 47 46 45 43 45 48 53 61 68 72.3 73.60 72.78 69.55 65.97 64.3 63.3
            6 1 61 60 58 57 55 54 53 53 54 57 57 57 58 61 64 66 66 65 65 67 68.2 69.56 68.69 67.57 65.56 63.8 62.3
            6 1 -9 -7 -5 -2 0 3 4 4 4 6 -1 -9 -10 -11 -12 -13 -15 -16 -16 -17 -17.4 -20.33 -20.90 -20.61 -19.22 -18.4 -16.9
            6 2 -11 -11 -11 -10 -10 -9 -9 -8 -7 6 4 3 1 8 15 28 42 51 59 68 74.2 76.74 75.92 72.79 72.96 76.7 80.7
            6 2 83 86 89 93 96 99 102 104 105 100 99 96 99 100 100 99 93 88 82 72 63.7 54.75 44.18 33.3 25.02 16.8 8.8
            6 3 -217 -221 -224 -228 -233 -238 -242 -246 -249 -246 -247 -247 -237 -228 -212 -198 -192 -185 -178 -170 -160.9 -151.34 -141.40 -129.85 -121.57 -115.7 -109.7
            6 3 2 4 5 8 11 14 19 25 33 16 33 48 60 68 72 75 71 69 69 67 65.1 63.63 61.54 58.74 52.76 48.9 46.9
            6 4 -58 -57 -54 -51 -46 -40 -32 -25 -18 -25 -16 -8 -1 4 2 1 4 4 3 -1 -5.9 -14.58 -22.83 -28.93 -36.06 -40.9 -44.9
            6 4 -35 -32 -29 -26 -22 -18 -16 -15 -15 -9 -12 -16 -20 -32 -37 -41 -43 -48 -52 -58 -61.2 -63.53 -66.26 -66.64 -64.40 -59.8 -55.8
            6 5 59 57 54 49 44 39 32 25 18 21 12 7 -2 1 3 6 14 16 18 19 16.9 14.58 13.10 13.14 13.60 14.9 16.9
            6 5 36 32 28 23 18 13 8 4 0 -16 -12 -12 -11 -8 -6 -4 -2 -1 1 1 0.7 0.24 3.02 7.35 8.96 10.9 14.4
            6 6 -90 -92 -95 -98 -101 -103 -104 -106 -107 -104 -105 -107 -113 -111 -112 -111 -108 -102 -96 -93 -90.4 -86.36 -78.09 -70.85 -64.80 -60.8 -56.3
            6 6 -69 -67 -65 -62 -57 -52 -46 -40 -33 -39 -30 -24 -17 -7 1 11 17 21 24 36 43.8 50.94 55.40 62.41 68.04 72.8 77.3
            7 0 70 70 71 72 73 73 74 74 74 70 65 65 67 75 72 71 72 74 77 77 79.0 79.88 80.44 81.29 80.54 79.6 79.1
            7 1 -55 -54 -54 -54 -54 -54 -54 -53 -53 -40 -55 -56 -56 -57 -57 -56 -59 -62 -64 -72 -74.0 -74.46 -75.00 -75.99 -76.63 -76.9 -77.4
            7 1 -45 -46 -47 -48 -49 -50 -51 -52 -52 -45 -35 -50 -55 -61 -70 -77 -82 -83 -80 -69 -64.6 -61.14 -57.80 -54.27 -51.50 -48.9 -45.9
            7 2 0 0 1 2 2 3 4 4 4 0 2 2 5 4 1 1 2 3 2 1 0.0 -1.65 -4.55 -6.79 -8.23 -8.8 -9.3
            7 2 -13 -14 -14 -14 -14 -14 -15 -17 -18 -18 -17 -24 -28 -27 -27 -26 -27 -27 -26 -25 -24.2 -22.57 -21.20 -19.53 -16.85 -14.4 -11.9
            7 3 34 33 32 31 29 27 25 23 20 0 1 10 15 13 14 16 21 24 26 28 33.3 38.73 45.24 51.82 56.45 59.3 61.8
            7 3 -10 -11 -12 -12 -13 -14 -14 -14 -14 2 0 -4 -6 -2 -4 -5 -5 -2 0 4 6.2 6.82 6.54 5.59 2.36 -1.0 -4.5
            7 4 -41 -41 -40 -38 -37 -35 -34 -33 -31 -29 -40 -32 -32 -26 -22 -14 -12 -6 -1 5 9.1 12.30 14.00 15.07 15.80 15.8 15.3
            7 4 -1 0 1 2 4 5 6 7 7 6 10 8 7 6 8 10 16 20 21 24 24.0 25.35 24.96 24.45 23.56 23.5 23.5
            7 5 -21 -20 -19 -18 -16 -14 -12 -11 -9 -10 -7 -11 -7 -6 -2 0 1 4 5 4 6.9 9.37 10.46 9.32 6.30 2.5 -1.5
            7 5 28 28 28 28 28 29 29 29 29 28 36 28 23 26 23 22 18 17 17 17 14.8 10.93 7.03 3.27 -2.19 -7.4 -11.9
            7 6 18 18 18 19 19 19 18 18 17 15 5 9 17 13 13 12 11 10 9 8 7.3 5.42 1.64 -2.88 -7.21 -11.2 -15.2
            7 6 -12 -12 -13 -15 -16 -17 -18 -19 -20 -17 -18 -20 -18 -23 -23 -23 -23 -23 -23 -24 -25.4 -26.32 -27.61 -27.5 -27.19 -25.1 -22.6
            7 7 6 6 6 6 6 6 6 6 5 29 19 18 8 1 -2 -5 -2 0 0 -2 -1.2 1.94 4.92 6.61 9.77 14.3 18.8
            7 7 -22 -22 -22 -22 -22 -21 -20 -19 -19 -22 -16 -18 -17 -12 -11 -12 -10 -7 -4 -6 -5.8 -4.64 -3.28 -2.32 -1.90 -2.2 -3.7
            8 0 11 11 11 11 11 11 11 11 11 13 22 11 15 13 14 14 18 21 23 25 24.4 24.80 24.41 23.98 23.66 23.1 22.6
            8 1 8 8 8 8 7 7 7 7 7 7 15 9 6 5 6 6 6 6 5 6 6.6 7.62 8.21 8.89 9.74 10.9 11.9
            8 1 8 8 8 8 8 8 8 8 8 12 5 10 11 7 7 6 7 8 10 11 11.9 11.20 10.84 10.04 8.43 7.2 5.7
            8 2 -4 -4 -4 -4 -3 -3 -3 -3 -3 -8 -4 -6 -4 -4 -2 -1 0 0 -1 -6 -9.2 -11.73 -14.50 -16.78 -17.49 -17.5 -17.5
            8 2 -14 -15 -15 -15 -15 -15 -15 -15 -14 -21 -22 -15 -14 -12 -15 -16 -18 -19 -19 -21 -21.5 -20.88 -20.03 -18.26 -15.23 -12.6 -10.6
            8 3 -9 -9 -9 -9 -9 -9 -9 -9 -10 -5 -1 -14 -11 -14 -13 -12 -11 -11 -10 -9 -7.9 -6.88 -5.59 -3.16 -0.49 2.0 4.0
            8 3 7 7 6 6 6 6 5 5 5 -12 0 5 7 9 6 4 4 5 6 8 8.5 9.83 11.83 13.18 12.83 11.5 10.0
            8 4 1 1 1 2 2 2 2 1 1 9 11 6 2 0 -3 -8 -7 -9 -12 -14 -16.6 -18.11 -19.34 -20.56 -21.07 -21.8 -22.3
            8 4 -13 -13 -13 -13 -14 -14 -14 -15 -15 -7 -21 -23 -18 -16 -17 -19 -22 -23 -22 -23 -21.5 -19.71 -17.41 -14.6 -11.76 -9.7 -7.7
            8 5 2 2 2 3 4 4 5 6 6 7 15 10 10 8 5 4 4 4 3 9 9.1 10.17 11.61 13.33 15.28 16.9 18.4
            8 5 5 5 5 5 5 5 5 5 5 2 -8 3 4 4 6 6 9 11 12 15 15.5 16.22 16.71 16.16 14.94 12.7 10.2
            8 6 -9 -8 -8 -8 -7 -7 -6 -6 -5 -10 -13 -7 -5 -1 0 0 3 4 4 6 7.0 9.36 10.85 11.76 13.65 14.9 15.4
            8 6 16 16 16 16 17 17 18 18 19 18 17 23 23 24 21 18 16 14 12 11 8.9 7.61 6.96 5.69 3.62 0.7 -2.3
            8 7 5 5 5 6 6 7 8 8 9 7 5 6 10 11 11 10 6 4 2 -5 -7.9 -11.25 -14.05 -15.98 -16.59 -16.8 -16.8
            8 7 -5 -5 -5 -5 -5 -5 -5 -5 -5 3 -4 -4 1 -3 -6 -10 -13 -15 -16 -16 -14.9 -12.76 -10.74 -9.1 -6.90 -5.2 -3.7
            8 8 8 8 8 8 8 8 8 7 7 2 -1 9 8 4 3 1 -1 -4 -6 -7 -7.0 -4.87 -3.54 -2.02 -0.34 1.0 2.5
            8 8 -18 -18 -18 -18 -19 -19 -19 -19 -19 -11 -17 -13 -20 -17 -16 -17 -15 -11 -10 -4 -2.1 -0.06 1.64 2.26 2.90 3.9 4.9
            9 0 8 8 8 8 8 8 8 8 8 5 3 4 4 8 8 7 5 5 4 4 5.0 5.58 5.50 5.33 5.03 4.7 4.7
            9 1 10 10 10 10 10 10 10 10 10 -21 -7 9 6 10 10 10 10 10 9 9 9.4 9.76 9.45 8.83 8.36 8.0 8.0
            9 1 -20 -20 -20 -20 -20 -20 -20 -20 -21 -27 -24 -11 -18 -22 -21 -21 -21 -21 -20 -20 -19.7 -20.11 -20.54 -21.77 -23.44 -24.8 -24.8
            9 2 1 1 1 1 1 1 1 1 1 1 -1 -4 0 2 2 2 1 1 1 3 3.0 3.58 3.45 3.02 2.84 3.0 3.0
            9 2 14 14 14 14 14 14 14 15 15 17 19 12 12 15 16 16 16 15 15 15 13.4 12.69 11.51 10.76 11.04 12.1 12.1
            9 3 -11 -11 -11 -11 -11 -11 -12 -12 -12 -11 -25 -5 -9 -13 -12 -12 -12 -12 -12 -10 -8.4 -6.94 -5.27 -3.22 -1.48 -0.2 -0.2
            9 3 5 5 5 5 5 5 5 5 5 29 12 7 2 7 6 7 9 9 11 12 12.5 12.67 12.75 11.74 9.86 8.3 8.3
            9 4 12 12 12 12 12 12 12 11 11 3 10 2 1 10 10 10 9 9 9 8 6.3 5.01 3.13 0.67 -1.14 -2.5 -2.5
            9 4 -3 -3 -3 -3 -3 -3 -3 -3 -3 -9 2 6 0 -4 -4 -4 -5 -6 -7 -6 -6.2 -6.72 -7.14 -6.74 -5.13 -3.4 -3.4
            9 5 1 1 1 1 1 1 1 1 1 16 5 4 4 -1 -1 -1 -3 -3 -4 -8 -8.9 -10.76 -12.38 -13.2 -13.22 -13.1 -13.1
            9 5 -2 -2 -2 -2 -2 -2 -2 -3 -3 4 2 -2 -3 -5 -5 -5 -6 -6 -7 -8 -8.4 -8.16 -7.42 -6.88 -6.20 -5.3 -5.3
            9 6 -2 -2 -2 -2 -2 -2 -2 -2 -2 -3 -5 1 -1 -1 0 -1 -1 -1 -2 -1 -1.5 -1.25 -0.76 -0.1 1.08 2.4 2.4
            9 6 8 8 8 8 9 9 9 9 9 9 8 10 9 10 10 10 9 9 9 8 8.4 8.10 7.97 7.79 7.79 7.2 7.2
            9 7 2 2 2 2 2 2 3 3 3 -4 -2 2 -2 5 3 4 7 7 7 10 9.3 8.76 8.43 8.68 8.82 8.6 8.6
            9 7 10 10 10 10 10 10 10 11 11 6 8 7 8 10 11 11 10 9 8 5 3.8 2.92 2.14 1.04 0.40 -0.6 -0.6
            9 8 -1 0 0 0 0 0 0 0 1 -3 3 2 3 1 1 1 2 1 1 -2 -4.3 -6.66 -8.42 -9.06 -9.23 -8.7 -8.7
            9 8 -2 -2 -2 -2 -2 -2 -2 -2 -2 1 -11 -6 0 -4 -2 -3 -6 -7 -7 -8 -8.2 -7.73 -6.08 -3.89 -1.44 0.8 0.8
            9 9 -1 -1 -1 -1 -1 -1 -2 -2 -2 -4 8 5 -1 -2 -1 -2 -5 -5 -6 -8 -8.2 -9.22 -10.08 -10.54 -11.86 -12.8 -12.8
            9 9 2 2 2 2 2 2 2 2 2 8 -7 5 5 1 1 1 2 2 2 3 4.8 6.01 7.01 8.44 9.60 9.8 9.8
            10 0 -3 -3 -3 -3 -3 -3 -3 -3 -3 -3 -8 -3 1 -2 -3 -3 -4 -4 -3 -3 -2.6 -2.17 -1.94 -2.01 -1.84 -1.3 -1.3
            10 1 -4 -4 -4 -4 -4 -4 -4 -4 -4 11 4 -5 -3 -3 -3 -3 -4 -4 -4 -6 -6.0 -6.12 -6.24 -6.26 -6.25 -6.4 -6.4
            10 1 2 2 2 2 2 2 2 2 2 5 13 -4 4 2 1 1 1 1 2 1 1.7 2.19 2.73 3.28 3.38 3.3 3.3
            10 2 2 2 2 2 2 2 2 2 2 1 -1 -1 4 2 2 2 2 3 2 2 1.7 1.42 0.89 0.17 -0.11 0.2 0.2
            10 2 1 1 1 1 1 1 1 1 1 1 -2 0 1 1 1 1 0 0 1 0 0.0 0.10 -0.10 -0.4 -0.18 0.1 0.1
            10 3 -5 -5 -5 -5 -5 -5 -5 -5 -5 2 13 2 0 -5 -5 -5 -5 -5 -5 -4 -3.1 -2.35 -1.07 0.55 1.66 2.0 2.0
            10 3 2 2 2 2 2 2 2 2 2 -20 -10 -8 0 2 3 3 3 3 3 4 4.0 4.46 4.71 4.55 3.50 2.5 2.5
            10 4 -2 -2 -2 -2 -2 -2 -2 -2 -2 -5 -4 -3 -1 -2 -1 -2 -2 -2 -2 -1 -0.5 -0.15 -0.16 -0.55 -0.86 -1.0 -1.0
            10 4 6 6 6 6 6 6 6 6 6 -1 2 -2 2 6 4 4 6 6 6 5 4.9 4.76 4.44 4.4 4.86 5.4 5.4
            10 5 6 6 6 6 6 6 6 6 6 -1 4 7 4 4 6 5 5 5 4 4 3.7 3.06 2.45 1.7 0.65 -0.5 -0.5
            10 5 -4 -4 -4 -4 -4 -4 -4 -4 -4 -6 -3 -4 -5 -4 -4 -4 -4 -4 -4 -5 -5.9 -6.58 -7.22 -7.92 -8.62 -9.0 -9.0
            10 6 4 4 4 4 4 4 4 4 4 8 12 4 6 4 4 4 3 3 3 2 1.0 0.29 -0.33 -0.67 -0.88 -0.9 -0.9
            10 6 0 0 0 0 0 0 0 0 0 6 6 1 1 0 0 -1 0 0 0 -1 -1.2 -1.01 -0.96 -0.61 -0.11 0.4 0.4
            10 7 0 0 0 0 0 0 0 0 0 -1 3 -2 1 0 1 1 1 1 1 2 2.0 2.06 2.13 2.13 1.88 1.5 1.5
            10 7 -2 -2 -2 -2 -2 -2 -2 -1 -1 -4 -3 -3 -1 -2 -1 -1 -1 -1 -2 -2 -2.9 -3.47 -3.95 -4.16 -4.26 -4.2 -4.2
            10 8 2 2 2 1 1 1 1 2 2 -3 2 6 -1 2 0 0 2 2 3 5 4.2 3.77 3.09 2.33 1.44 0.9 0.9
            10 8 4 4 4 4 4 4 4 4 4 -2 6 7 6 3 3 3 4 4 3 1 0.2 -0.86 -1.99 -2.85 -3.43 -3.8 -3.8
            10 9 2 2 2 2 3 3 3 3 3 5 10 -2 2 2 3 3 3 3 3 1 0.3 -0.21 -1.03 -1.8 -2.38 -2.6 -2.6
            10 9 0 0 0 0 0 0 0 0 0 0 11 -1 0 0 1 1 0 0 -1 -2 -2.2 -2.31 -1.97 -1.12 -0.10 0.9 0.9
            10 10 0 0 0 0 0 0 0 0 0 -2 3 0 0 0 -1 -1 0 0 0 0 -1.1 -2.09 -2.80 -3.59 -3.84 -3.9 -3.9
            10 10 -6 -6 -6 -6 -6 -6 -6 -6 -6 -2 8 -3 -7 -6 -4 -5 -6 -6 -6 -7 -7.4 -7.93 -8.31 -8.72 -8.84 -9.0 -9.0
            11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2.7 2.95 3.05 3 2.96 3.0 3.0
            11 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -1.7 -1.60 -1.48 -1.4 -1.36 -1.4 -1.4
            11 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.1 0.26 0.13 0 -0.02 0.0 0.0
            11 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -1.9 -1.88 -2.03 -2.3 -2.51 -2.5 -2.5
            11 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1.3 1.44 1.67 2.11 2.50 2.8 2.8
            11 3 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1.5 1.44 1.65 2.08 2.31 2.4 2.4
            11 3 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.9 -0.77 -0.66 -0.6 -0.55 -0.6 -0.6
            11 4 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.1 -0.31 -0.51 -0.79 -0.85 -0.6 -0.6
            11 4 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -2.6 -2.27 -1.76 -1.05 -0.39 0.1 0.1
            11 5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.1 0.29 0.54 0.58 0.28 0.0 0.0
            11 5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.9 0.90 0.85 0.76 0.62 0.5 0.5
            11 6 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.7 -0.79 -0.79 -0.7 -0.66 -0.6 -0.6
            11 6 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.7 -0.58 -0.39 -0.2 -0.21 -0.3 -0.3
            11 7 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.7 0.53 0.37 0.14 -0.07 -0.1 -0.1
            11 7 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -2.8 -2.69 -2.51 -2.12 -1.66 -1.2 -1.2
            11 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1.7 1.80 1.79 1.7 1.44 1.1 1.1
            11 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.9 -1.08 -1.27 -1.44 -1.60 -1.7 -1.7
            11 9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.1 0.16 0.12 -0.22 -0.59 -1.0 -1.0
            11 9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -1.2 -1.58 -2.11 -2.57 -2.98 -2.9 -2.9
            11 10 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1.2 0.96 0.75 0.44 0.18 -0.1 -0.1
            11 10 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -1.9 -1.90 -1.94 -2.01 -1.97 -1.8 -1.8
            11 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 4.0 3.99 3.75 3.49 3.09 2.6 2.6
            11 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.9 -1.39 -1.86 -2.34 -2.51 -2.3 -2.3
            12 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -2.2 -2.15 -2.12 -2.09 -2.00 -2.0 -2.0
            12 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.3 -0.29 -0.21 -0.16 -0.13 -0.1 -0.1
            12 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.55 -0.87 -1.08 -1.15 -1.2 -1.2
            12 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.2 0.21 0.30 0.46 0.43 0.4 0.4
            12 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.23 0.27 0.37 0.52 0.6 0.6
            12 3 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.9 0.89 1.04 1.23 1.28 1.2 1.2
            12 3 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2.5 2.38 2.13 1.75 1.37 1.0 1.0
            12 4 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.2 -0.38 -0.63 -0.89 -1.14 -1.2 -1.2
            12 4 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -2.6 -2.63 -2.49 -2.19 -1.81 -1.5 -1.5
            12 5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.9 0.96 0.95 0.85 0.71 0.6 0.6
            12 5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.7 0.61 0.49 0.27 0.08 0.0 0.0
            12 6 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.5 -0.30 -0.11 0.1 0.31 0.5 0.5
            12 6 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.40 0.59 0.72 0.71 0.6 0.6
            12 7 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.46 0.52 0.54 0.49 0.5 0.5
            12 7 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.0 0.01 0.00 -0.09 -0.15 -0.2 -0.2
            12 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.3 -0.35 -0.39 -0.37 -0.26 -0.1 -0.1
            12 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.0 0.02 0.13 0.29 0.55 0.8 0.8
            12 9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.36 -0.37 -0.43 -0.47 -0.5 -0.5
            12 9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.28 0.27 0.23 0.16 0.1 0.1
            12 10 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.1 0.08 0.21 0.22 0.09 -0.2 -0.2
            12 10 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.9 -0.87 -0.86 -0.89 -0.93 -0.9 -0.9
            12 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.2 -0.49 -0.77 -0.94 -1.13 -1.2 -1.2
            12 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.34 -0.23 -0.16 -0.04 0.1 0.1
            12 12 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.08 0.04 -0.03 -0.33 -0.7 -0.7
            12 12 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.8 0.88 0.87 0.72 0.52 0.2 0.2
            13 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.2 -0.16 -0.09 -0.02 0.08 0.2 0.2
            13 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.9 -0.88 -0.89 -0.92 -0.93 -0.9 -0.9
            13 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.9 -0.76 -0.87 -0.88 -0.88 -0.9 -0.9
            13 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.30 0.31 0.42 0.53 0.6 0.6
            13 2 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.2 0.33 0.30 0.49 0.64 0.7 0.7
            13 3 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.1 0.28 0.42 0.63 0.72 0.7 0.7
            13 3 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1.8 1.72 1.66 1.56 1.40 1.2 1.2
            13 4 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.43 -0.45 -0.42 -0.30 -0.2 -0.2
            13 4 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.54 -0.59 -0.5 -0.38 -0.3 -0.3
            13 5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1.3 1.18 1.08 0.96 0.75 0.5 0.5
            13 5 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -1.0 -1.07 -1.14 -1.24 -1.31 -1.3 -1.3
            13 6 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.37 -0.31 -0.19 -0.01 0.1 0.1
            13 6 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.1 -0.04 -0.07 -0.1 -0.09 -0.1 -0.1
            13 7 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.7 0.75 0.78 0.81 0.76 0.7 0.7
            13 7 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.7 0.63 0.54 0.42 0.29 0.2 0.2
            13 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.4 -0.26 -0.18 -0.13 -0.05 0.0 0.0
            13 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.21 0.10 -0.04 -0.11 -0.2 -0.2
            13 9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.35 0.38 0.38 0.37 0.3 0.3
            13 9 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.6 0.53 0.49 0.48 0.47 0.5 0.5
            13 10 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.1 -0.05 0.02 0.08 0.13 0.2 0.2
            13 10 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.3 0.38 0.44 0.48 0.54 0.6 0.6
            13 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.4 0.41 0.42 0.46 0.45 0.4 0.4
            13 11 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.2 -0.22 -0.25 -0.3 -0.41 -0.6 -0.6
            13 12 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.0 -0.10 -0.26 -0.35 -0.46 -0.5 -0.5
            13 12 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.5 -0.57 -0.53 -0.43 -0.36 -0.3 -0.3
            13 13 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.1 -0.18 -0.26 -0.36 -0.40 -0.4 -0.4
            13 13 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -0.9 -0.82 -0.79 -0.71 -0.60 -0.5 -0.5   
            """;
    }

    private static double[,] LegendrePoly(int nmax, double[] theta)
    {
        var costh = theta.Select(t => Math.Cos(DegreesToRadians(t))).ToArray();
        var sinth = costh.Select(c => Math.Sqrt(1 - c * c)).ToArray();

        var Pnm = new double[nmax + 1, nmax + 2];
        Pnm[0, 0] = 1;
        Pnm[1, 1] = sinth[0];

        var rootn = Enumerable.Range(0, 2 * nmax * nmax + 1).Select(x => Math.Sqrt(x)).ToArray();

        for (var m = 0; m < nmax; m++)
        {
            var Pnm_tmp = rootn[m + m + 1] * Pnm[m, m];
            Pnm[m + 1, m] = costh[0] * Pnm_tmp;

            if (m > 0) Pnm[m + 1, m + 1] = sinth[0] * Pnm_tmp / rootn[m + m + 2];

            for (var n = m + 2; n <= nmax; n++)
            {
                var d = n * n - m * m;
                var e = n + n - 1;
                Pnm[n, m] = (e * costh[0] * Pnm[n - 1, m] - rootn[d - e] * Pnm[n - 2, m]) / rootn[d];
            }
        }

        Pnm[0, 2] = -Pnm[1, 1];
        Pnm[1, 2] = Pnm[1, 0];
        for (var n = 2; n <= nmax; n++)
        {
            //Change from the python with (double) added for conversion
            Pnm[0, n + 1] = -Math.Sqrt((n * n + n) / (double)2) * Pnm[n, 1];
            Pnm[1, n + 1] = (Math.Sqrt(2 * (n * n + n)) * Pnm[n, 0] - Math.Sqrt(n * n + n - 2) * Pnm[n, 2]) / 2;

            for (var m = 2; m < n; m++)
                Pnm[m, n + 1] = 0.5 * (Math.Sqrt((n + m) * (n - m + 1)) * Pnm[n, m - 1] -
                                       Math.Sqrt((n + m + 1) * (n - m)) * Pnm[n, m + 1]);

            Pnm[n, n + 1] = Math.Sqrt(2 * n) * Pnm[n, n - 1] / 2;
        }

        return Pnm;
    }

    public static (double[] time, double[,] coeffs, Dictionary<string, object> parameters) LoadInternalShcModel(
        InternalShcModel model = InternalShcModel.Igrf14Model)
    {
        var data = model switch
        {
            InternalShcModel.Igrf13Model => Igrf13Model(),
            InternalShcModel.Igrf14Model => Igrf14Model(),
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
        };

        var lines = data.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var values = new List<object>();
        var dataList = new List<double>();

        var firstLine = true;
        var name = "SHC Name Unknown";

        foreach (var line in lines)
        {
            if (line.StartsWith("#"))
            {
                if (firstLine)
                {
                    name = line;
                    firstLine = false;
                }

                continue;
            }

            var readLine = line.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(double.Parse)
                .ToArray();
            if (readLine.Length == 7)
            {
                values.Add(name);
                readLine.Select(Convert.ToInt32).Cast<object>().ToList().ForEach(values.Add);
            }
            else
            {
                dataList.AddRange(readLine);
            }
        }

        // Unpack parameter line
        var keys = new[] { "SHC", "nmin", "nmax", "N", "order", "step", "start_year", "end_year" };
        var parameters = keys.Zip(values, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

        var time = dataList.Take((int)parameters["N"]).ToArray();
        var coeffsData = dataList.Skip((int)parameters["N"]).ToArray();
        var coeffs = new double[coeffsData.Length / ((int)parameters["N"] + 2), (int)parameters["N"]];
        for (var i = 0; i < coeffs.GetLength(0); i++)
        for (var j = 0; j < coeffs.GetLength(1); j++)
            coeffs[i, j] = coeffsData[i * ((int)parameters["N"] + 2) + j + 2];

        return (time, coeffs, parameters);
    }

    public static (double[] time, double[,] coeffs, Dictionary<string, object> parameters) LoadShcFile(
        string filepath, bool? leapYear = null)
    {
        leapYear ??= true;

        var data = new List<double>();
        string[] lines = File.ReadAllLines(filepath);
        var name = Path.GetFileName(filepath);
        List<object> values = [];

        foreach (var line in lines)
        {
            if (line.StartsWith("#"))
                continue;

            var readLine = line.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(double.Parse)
                .ToArray();
            if (readLine.Length == 7)
            {
                values.Add(name);
                values.AddRange(readLine.Select(Convert.ToInt32).Cast<object>());
            }
            else
            {
                data.AddRange(readLine);
            }
        }

        // Unpack parameter line
        var keys = new[] { "SHC", "nmin", "nmax", "N", "order", "step", "start_year", "end_year" };
        var parameters = keys.Zip(values, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);

        var time = data.Take((int)parameters["N"]).ToArray();
        var coeffsData = data.Skip((int)parameters["N"]).ToArray();
        var coeffs = new double[coeffsData.Length / ((int)parameters["N"] + 2), (int)parameters["N"]];
        for (var i = 0; i < coeffs.GetLength(0); i++)
        for (var j = 0; j < coeffs.GetLength(1); j++)
            coeffs[i, j] = coeffsData[i * ((int)parameters["N"] + 2) + j + 2];

        return (time, coeffs, parameters);
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / Math.PI;
    }

    public static (double[] B_radius, double[] B_theta, double[] B_phi) SynthValues(
        double[] coeffs, double radiusInput, double theta, double phi, int? nmax = null, int? nmin = null,
        bool grid = false)
    {
        return SynthValues(coeffs, [radiusInput], [theta], [phi], nmax, nmin, grid);
    }

    public static (double[] B_radius, double[] B_theta, double[] B_phi) SynthValues(
        double[] coeffs, double[] radiusInput, double[] theta, double[] phi, int? nmax = null, int? nmin = null,
        bool grid = false)
    {
        // Ensure inputs are arrays
        var radius = radiusInput.Select(r => r / 6371.2).ToArray(); // Earth's average radius

        if (theta.Min() <= 0.0 || theta.Max() >= 180.0)
        {
            if (theta.Min() == 0.0 || theta.Max() == 180.0)
                Console.WriteLine("The geographic poles are included.");
            else
                throw new ArgumentException("Colatitude outside bounds [0, 180].");
        }

        nmin ??= 1;
        if (nmin <= 0) throw new ArgumentException("Only positive nmin allowed.");

        var nmax_coeffs = (int)Math.Sqrt(coeffs.Length + 1) - 1;
        nmax ??= nmax_coeffs;
        if (nmax <= 0) throw new ArgumentException("Only positive nmax allowed.");

        if (nmax > nmax_coeffs)
        {
            Console.WriteLine(
                $"Supplied nmax = {nmax} and nmin = {nmin} is incompatible with number of model coefficients. Using nmax = {nmax_coeffs} instead.");
            nmax = nmax_coeffs;
        }

        if (nmax < nmin) throw new ArgumentException($"Nothing to compute: nmax < nmin ({nmax} < {nmin}).");

        // Handle grid option
        if (grid)
        {
            theta = theta.Select(t => t).ToArray();
            phi = phi.Select(p => p).ToArray();
        }

        // Initialize radial dependence given the source
        var r_n = radius.Select(r => Math.Pow(r, -(nmin.Value + 2))).ToArray();

        // Compute associated Legendre polynomials
        var Pnm = LegendrePoly(nmax.Value, theta);

        // Save sinth for fast access
        var sinth = Pnm[1, 1];

        // Calculate cos(m*phi) and sin(m*phi)
        phi = phi.Select(DegreesToRadians).ToArray();
        var cmp = new double[nmax.Value + 1, phi.Length];
        var smp = new double[nmax.Value + 1, phi.Length];
        for (var m = 0; m <= nmax.Value; m++)
        for (var i = 0; i < phi.Length; i++)
        {
            cmp[m, i] = Math.Cos(m * phi[i]);
            smp[m, i] = Math.Sin(m * phi[i]);
        }

        // Allocate arrays in memory
        var B_radius = new double[radius.Length];
        var B_theta = new double[radius.Length];
        var B_phi = new double[radius.Length];

        var num = nmin.Value * nmin.Value - 1;
        for (var n = nmin.Value; n <= nmax.Value; n++)
        {
            for (var i = 0; i < radius.Length; i++)
            {
                B_radius[i] += (n + 1) * Pnm[n, 0] * r_n[i] * coeffs[num];
                B_theta[i] += -Pnm[0, n + 1] * r_n[i] * coeffs[num];
            }

            num += 1;

            for (var m = 1; m <= n; m++)
            {
                for (var i = 0; i < radius.Length; i++)
                {
                    B_radius[i] += (n + 1) * Pnm[n, m] * r_n[i] *
                                   (coeffs[num] * cmp[m, i] + coeffs[num + 1] * smp[m, i]);
                    B_theta[i] +=
                        -Pnm[m, n + 1] * r_n[i] * (coeffs[num] * cmp[m, i] + coeffs[num + 1] * smp[m, i]);

                    var div_Pnm = theta[i] == 0.0 ? Pnm[m, n + 1] : Pnm[n, m] / sinth;
                    div_Pnm = theta[i] == 180.0 ? -Pnm[m, n + 1] : div_Pnm;

                    B_phi[i] += m * div_Pnm * r_n[i] * (coeffs[num] * smp[m, i] - coeffs[num + 1] * cmp[m, i]);
                }

                num += 2;
            }

            r_n = r_n.Select(r => r / radius[0]).ToArray(); // equivalent to r_n = radius**(-(n+2))
        }

        return (B_radius, B_theta, B_phi);
    }

    public static (double D, double H, double I, double F) Xyz2Dhif(double x, double y, double z)
    {
        var hsq = x * x + y * y;
        var hoz = Math.Sqrt(hsq);
        var eff = Math.Sqrt(hsq + z * z);
        var dec = Math.Atan2(y, x);
        var inc = Math.Atan2(z, hoz);

        return (RadiansToDegrees(dec), hoz, RadiansToDegrees(inc), eff);
    }

    public static (double Ddot, double Hdot, double Idot, double Fdot) Xyz2DhifSv(double x, double y, double z,
        double xdot, double ydot, double zdot)
    {
        var h2 = x * x + y * y;
        var h = Math.Sqrt(h2);
        var f2 = h2 + z * z;
        var hdot = (x * xdot + y * ydot) / h;
        var fdot = (x * xdot + y * ydot + z * zdot) / Math.Sqrt(f2);
        //TODO: The original Python seems to calculate the same value as 
        // http://www.geomag.bgs.ac.uk/data_service/models_compass/igrf_calc.html but
        // with the opposite sign. As far as I can understand the sign from the
        // Python is incorrect, but it could also be that I accidentally modified
        // the code, or I am misunderstanding inputs?
        var dtest = RadiansToDegrees((xdot * y - ydot * x) / h2);
        var itest = RadiansToDegrees((hdot * z - h * zdot) / f2);

        var ddot = RadiansToDegrees((ydot * x - xdot * y) / h2) * 60;
        var idot = RadiansToDegrees((h * zdot - hdot * z) / f2) * 60;

        return (ddot, hdot, idot, fdot);
    }
}