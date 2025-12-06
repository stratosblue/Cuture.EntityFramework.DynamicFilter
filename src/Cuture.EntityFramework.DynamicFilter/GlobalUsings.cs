#if NET10_0_OR_GREATER

global using ParameterValues = System.Collections.Generic.Dictionary<System.String, System.Object?>;

#else

global using ParameterValues = Microsoft.EntityFrameworkCore.Query.Internal.IParameterValues;

#endif
