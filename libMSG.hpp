#ifndef LIBMSG_HPP
#define LIBMSG_HPP

#include <fstream>
#include <vector>
#include <string>
//#include <string_view>
#include <filesystem>

namespace fs = std::filesystem;

namespace Patapon {
	enum class PataponMessageFormat {
		MSG = 1,
		TXT = 0
	};
	// Please note that any changes made to the instance of MSG don't immediately affect the source:
	// The stream of characters or buffer won't be modified at all, whereas the file can be overwritten, but that required a cetrain push.
	class MSG { 
	private:
		int magic;
		int entries_count;
		std::vector<int> offsets;
		std::vector <std::wstring> entries;
		bool from_file;
		fs::path original_file;
	public:
		MSG() = default; //default constructor
		
		MSG(std::ifstream& stream); // Reads MSG from file stream opened with std::ios::binary | std::ios::ate flags without reading it into memory
		MSG(const std::vector <char>& buffer); // Reads MSG from a buffer to where it was previously written
		MSG(const std::string& filename); // Opens file and then acts as the previous constructor
		MSG(const fs::path& path); // The same thing, pretty much

		//MSG(const std::wstring& filename);
		int getMagic() const;
		void setMagic(int _magic);
		int count() const;
		std::wstring& operator[] (size_t index);
		std::wstring operator[] (size_t index) const;

		void save_to_file(const std::string& filename, PataponMessageFormat format); // These two methods can be used if there is no file with given path,
		void save_to_file(const fs::path& path, PataponMessageFormat format); // whereas the next one assumes the file still exists and makes small changes
		void update_source_file(); // Will throw if the constructor was not from const std::string& or const fs::path&
	};

	class MSGReader {
	private:
		/*class EntryGetter {
			void * start;
			EntryGetter(void* MSG_start) : start(MSG_start) {};
			const std::string operator[](int index) const {

			}
		};*/
	public:
		static int getEntriesCount(void* MSG_start);
		static int getMagic(void* MSG_start);
		//static EntryGetter getEntry(void* MSG_start);
		static const std::wstring getWStringEntry(void* MSG_start, size_t index);
	};
}

#endif