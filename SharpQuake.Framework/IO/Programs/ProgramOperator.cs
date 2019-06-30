using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework
{
    public enum ProgramOperator
    {
        OP_DONE,
        OP_MUL_F,
        OP_MUL_V,
        OP_MUL_FV,
        OP_MUL_VF,
        OP_DIV_F,
        OP_ADD_F,
        OP_ADD_V,
        OP_SUB_F,
        OP_SUB_V,

        OP_EQ_F,
        OP_EQ_V,
        OP_EQ_S,
        OP_EQ_E,
        OP_EQ_FNC,

        OP_NE_F,
        OP_NE_V,
        OP_NE_S,
        OP_NE_E,
        OP_NE_FNC,

        OP_LE,
        OP_GE,
        OP_LT,
        OP_GT,

        OP_LOAD_F,
        OP_LOAD_V,
        OP_LOAD_S,
        OP_LOAD_ENT,
        OP_LOAD_FLD,
        OP_LOAD_FNC,

        OP_ADDRESS,

        OP_STORE_F,
        OP_STORE_V,
        OP_STORE_S,
        OP_STORE_ENT,
        OP_STORE_FLD,
        OP_STORE_FNC,

        OP_STOREP_F,
        OP_STOREP_V,
        OP_STOREP_S,
        OP_STOREP_ENT,
        OP_STOREP_FLD,
        OP_STOREP_FNC,

        OP_RETURN,
        OP_NOT_F,
        OP_NOT_V,
        OP_NOT_S,
        OP_NOT_ENT,
        OP_NOT_FNC,
        OP_IF,
        OP_IFNOT,
        OP_CALL0,
        OP_CALL1,
        OP_CALL2,
        OP_CALL3,
        OP_CALL4,
        OP_CALL5,
        OP_CALL6,
        OP_CALL7,
        OP_CALL8,
        OP_STATE,
        OP_GOTO,
        OP_AND,
        OP_OR,

        OP_BITAND,
        OP_BITOR
    }
}
