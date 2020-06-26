#
#
#

from conans import ConanFile, CMake
import os


def is_windows():
    return os.name == 'nt'


class AuthService(ConanFile):
    settings = "os", "compiler", "build_type", "arch"
    generators = "cmake"
    default_options = {
        "libpq:with_openssl": False,
        "paho-mqtt-cpp:ssl": True,
        "paho-mqtt-c:ssl": True,
        "libpq:shared": False,
        "libpqxx:shared": False,
    }

    def configure(self):
        if is_windows():
            return
			
        self.options["mongo-c-driver"].shared = True

    def requirements(self):
        if is_windows():
            self.requires("mongo-c-driver/1.16.1@bincrafters/stable")
            self.requires("paho-mqtt-cpp/1.1", override=False)
            self.requires("libpqxx/7.0.5", override=False)
            self.requires("libpq/11.5", override=True)
            self.requires("boost/1.73.0")
            self.requires("zlib/1.2.11", override=True)
            self.requires("re2/20200601")
            self.requires("protobuf/3.11.4")
        else:
            self.requires("mongo-c-driver/1.16.1@bincrafters/stable")
            self.requires("boost/1.73.0")
            self.requires("zlib/1.2.11", override=True)
            self.requires("paho-mqtt-cpp/1.1", override=False)
            self.requires("paho-mqtt-c/1.3.1", override=True)
            self.requires("libpq/11.5", override=True)
            self.requires("libpqxx/7.0.5", override=False)
            self.requires("openssl/1.1.1g", override=True)
            self.requires("re2/20200601")
            self.requires("protobuf/3.11.4")

    def imports(self):
        self.copy('libpq.so*', 'libdist', 'lib')
        self.copy('lib*mongo*.so*', 'libdist', 'lib')
        self.copy('lib*bson*.so*', 'libdist', 'lib')

    def build(self):
        cmake = CMake(self)
        cmake.configure()
        cmake.build()
        cmake.install()
