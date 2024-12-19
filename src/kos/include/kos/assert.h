#ifndef KOS_ASSERT_H
#define KOS_ASSERT_H

#include <assert.h>

#define KOS_ASSERT(Predicate, Message) do { assert((Predicate) && "" Message ""); } while (0)
#define KOS_TODO(Message) KOS_ASSERT(false, "TODO: " Message)
#define KOS_UNREACHABLE(Message) KOS_ASSERT(false, "Reached unreachable code: " Message);

#endif // KOS_ASSERT_H
