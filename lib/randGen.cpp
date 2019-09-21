#include <random>
#include "pcg_random.hpp"

extern "C" pcg32_k64* initRand()
{
    pcg_extras::seed_seq_from<std::random_device> seed_source;

    pcg32_k64* randGen = new pcg32_k64(seed_source);

    return randGen;
}

extern "C" uint32_t getRand(pcg32_k64* randGen)
{
    return randGen->operator()();
}

extern "C" void freeRand(pcg32_k64* randGen)
{
    delete randGen;
}