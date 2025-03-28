#ifndef CC_STD
#    define CC_STD(Id, Name, Lang, Desc, Feats)
#endif // CC_STD

#ifndef CC_STD_ALIAS
#    define CC_STD_ALIAS(Id, Alias)
#endif // CC_STD_ALIAS

#ifndef CC_STD_DEPR
#    define CC_STD_DEPR(Id, Alias) CC_STD_ALIAS(Id, Alias)
#endif // CC_STD_DEPR

CC_STD(C89, "c89", CC_LANG_C, "ISO C 1990", CCLY_FEAT_NONE)
CC_STD_ALIAS(C89, "c90")
CC_STD_ALIAS(C89, "iso9899:1990")
CC_STD(C94, "iso9899:199409", CC_LANG_C, "ISO C 1990 with amendment 1", CCLY_FEAT_DIGRAPHS)
CC_STD(GNU89, "gnu89", CC_LANG_C, "ISO C 1990 with GNU extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_GNUMODE)
CC_STD_ALIAS(GNU89, "gnu90")
CC_STD(CHOIR89, "choir89", CC_LANG_C, "ISO C 1990 with Choir extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_CHOIRMODE)
CC_STD_ALIAS(CHOIR89, "choir90")
CC_STD(C99, "c99", CC_LANG_C, "ISO C 1999", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_HEXFLOAT)
CC_STD_ALIAS(C99, "iso9899:1999")
CC_STD_DEPR(C99, "c9x")
CC_STD_DEPR(C99, "iso9899:199x")
CC_STD(GNU99, "gnu99", CC_LANG_C, "ISO C 1999 with GNU extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_GNUMODE | CCLY_FEAT_HEXFLOAT)
CC_STD_DEPR(GNU99, "gnu9x")
CC_STD(CHOIR99, "choir99", CC_LANG_C, "ISO C 1999 with Choir extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_CHOIRMODE | CCLY_FEAT_HEXFLOAT)
CC_STD(C11, "c11", CC_LANG_C, "ISO C 2011", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_HEXFLOAT)
CC_STD_ALIAS(C11, "iso9899:2011")
CC_STD_DEPR(C11, "c1x")
CC_STD_DEPR(C11, "iso9899:201x")
CC_STD(GNU11, "gnu11", CC_LANG_C, "ISO C 2011 with GNU extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_GNUMODE | CCLY_FEAT_HEXFLOAT)
CC_STD_DEPR(GNU11, "gnu1x")
CC_STD(CHOIR11, "choir11", CC_LANG_C, "ISO C 2011 with Choir extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_CHOIRMODE | CCLY_FEAT_HEXFLOAT)
CC_STD(C17, "c17", CC_LANG_C, "ISO C 2017", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_HEXFLOAT)
CC_STD_ALIAS(C17, "iso9899:2017")
CC_STD_ALIAS(C17, "c18")
CC_STD_ALIAS(C17, "iso9899:2018")
CC_STD(GNU17, "gnu17", CC_LANG_C, "ISO C 2017 with GNU extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_GNUMODE | CCLY_FEAT_HEXFLOAT)
CC_STD_ALIAS(GNU17, "gnu18")
CC_STD(CHOIR17, "choir17", CC_LANG_C, "ISO C 2017 with Choir extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_CHOIRMODE | CCLY_FEAT_HEXFLOAT)
CC_STD(C23, "c23", CC_LANG_C, "ISO C 2023", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_C23 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_HEXFLOAT)
CC_STD_DEPR(C23, "c2x")
CC_STD(GNU23, "gnu23", CC_LANG_C, "ISO C 2023 with GNU extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_C23 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_GNUMODE | CCLY_FEAT_HEXFLOAT)
CC_STD_DEPR(GNU23, "gnu2x")
CC_STD(CHOIR23, "choir23", CC_LANG_C, "ISO C 2023 with Choir extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_C23 | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_CHOIRMODE | CCLY_FEAT_HEXFLOAT)
CC_STD(C2Y, "c2y", CC_LANG_C, "Working Draft for ISO C2y", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_C23 | CCLY_FEAT_C2Y | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_HEXFLOAT)
CC_STD(GNU2Y, "gnu2y", CC_LANG_C, "Working Draft for ISO C2y with GNU extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_C23 | CCLY_FEAT_C2Y | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_GNUMODE | CCLY_FEAT_HEXFLOAT)
CC_STD(CHOIR2Y, "choir2y", CC_LANG_C, "Working Draft for ISO C2y with Choir extensions", CCLY_FEAT_LINE_COMMENT | CCLY_FEAT_C99 | CCLY_FEAT_C11 | CCLY_FEAT_C17 | CCLY_FEAT_C23 | CCLY_FEAT_C2Y | CCLY_FEAT_DIGRAPHS | CCLY_FEAT_CHOIRMODE | CCLY_FEAT_HEXFLOAT)

#undef CC_STD
#undef CC_STD_ALIAS
#undef CC_STD_DEPR
