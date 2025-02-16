#include <laye/laye.h>

CHOIR_API const char* ly_token_kind_name_get(ly_token_kind kind) {
    switch (kind) {
        default: return "[unknown Laye source token kind]";
        // clang-format off
#define LY_TOKEN(Name)   case LY_TK_##Name: return #Name;
#define LY_TOKEN_MISSING case LY_TK_MISSING: return "MISSING";
#include <laye/tokens.inc>
        // clang-format on
    }
}
