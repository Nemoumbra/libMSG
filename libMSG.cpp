#ifndef LIBMSG_CPP
#define LIBMSG_CPP

#include "libMSG.hpp"
#include <fstream>
#include <vector>
#include <string>
#include <filesystem>

namespace Patapon {

	int MSGReader::getEntriesCount(void* MSG_start) {
		return *reinterpret_cast<int*>(MSG_start);
	}
	int MSGReader::getMagic(void* MSG_start) {
		return *(reinterpret_cast<int*>(MSG_start) + 1);
	}
	const std::wstring MSGReader::getWStringEntry(void* MSG_start, size_t index) {

		if (index >= *reinterpret_cast<int*>(MSG_start)) { // TO DO: isn't that a comparison between a signed and unsigned objects?
			//return std::wstring(L"no_message");
			throw std::runtime_error("getWStringEntry: \"index\" argument out of range");
		}
		int* offset_address = reinterpret_cast<int*>(MSG_start) + 2 + index;

		return reinterpret_cast<wchar_t*> (reinterpret_cast<char*>(MSG_start) + *offset_address);
	}

	
	MSG::MSG(const std::vector <char>& buffer) : // TO DO: buffer.data() may return nullptr if buffer is empty
		magic(*(reinterpret_cast<const int*>(buffer.data()) + 1)),
		entries_count(*reinterpret_cast<const int*>(buffer.data())),
		offsets(),
		entries(),
		from_file(false),
		original_file("") {
		//NB! std::wstring(const wchar_t*) copies the data, is it necessary to think of a workaround?
		int* offset = reinterpret_cast<int*> (const_cast<char*> (buffer.data())) + 1; //Pointing at magic
		for (int i = 0; i < entries_count; ++i) { // TO DO: std::wstring constructor may throw as it is not noexcept; push_back may also throw
			/*if (i == 75) {
				i = 75;
			}*/
			offsets.push_back(*(++offset));
			entries.push_back(std::wstring(reinterpret_cast<const wchar_t*> (buffer.data() + offsets[i])));
		}
	}
	MSG::MSG(std::ifstream& stream) : magic(0), entries_count(0), offsets(), entries(), from_file(false), original_file("") { // TO DO: exception safety
		int filesize = static_cast<int> (stream.tellg());
		stream.seekg(0);
		stream.read(reinterpret_cast <char*> (&entries_count), 4);
		stream.read(reinterpret_cast <char*> (&magic), 4);
		int offset;
		int max_length = 0;

		//First iteration doesn't make comparisons
		stream.read(reinterpret_cast <char*> (&offset), 4);
		offsets.push_back(offset);
		
		for (int i = 1; i < entries_count; ++i) {
			stream.read(reinterpret_cast <char*> (&offset), 4);
			offsets.push_back(offset);
			if (max_length < offset - offsets[i - 1]) {
				max_length = offset + offsets[i - 1];
			}
		}
		if (max_length < filesize - offsets[entries_count - 1]) {
			max_length = filesize - offsets[entries_count - 1];
		}
		//Skip next 4 zero bytes
		stream.read(reinterpret_cast <char*> (&offset), 4);

		char* buffer = new char[max_length + 1];
		for (int i = 0; i < entries_count - 1; ++i) {
			stream.read(buffer, offsets[i + 1] - offsets[i]);
			entries.push_back(std::wstring(reinterpret_cast <wchar_t*> (buffer)));
			// Here copying is necessary as the buffer is constantly refreshed
		}
		//The last iteration doesn't have an "i+1" offset
		stream.read(buffer, filesize - offsets[entries_count - 1]);
		entries.push_back(std::wstring(reinterpret_cast <wchar_t*> (buffer)));
		delete[] buffer;
	}
	MSG::MSG(const std::string& filename) : magic(0), entries_count(0), offsets(), entries(), from_file(false), original_file("") { // TO DO: exception safety
		std::ifstream source(filename, std::ios::binary | std::ios::ate);
		if (!source.is_open()) {
			throw std::runtime_error("MSG: can't access " + filename);
		}

		int filesize = static_cast<int> (source.tellg());
		source.seekg(0);
		source.read(reinterpret_cast <char*> (&entries_count), 4);
		source.read(reinterpret_cast <char*> (&magic), 4);
		int offset;
		int max_length = 0;

		//First iteration doesn't make comparisons
		source.read(reinterpret_cast <char*> (&offset), 4);
		offsets.push_back(offset);

		for (int i = 1; i < entries_count; ++i) {
			source.read(reinterpret_cast <char*> (&offset), 4);
			offsets.push_back(offset);
			if (max_length < offset - offsets[i - 1]) {
				max_length = offset + offsets[i - 1];
			}
		}
		if (max_length < filesize - offsets[entries_count - 1]) {
			max_length = filesize - offsets[entries_count - 1];
		}
		//Skip next 4 zero bytes
		source.read(reinterpret_cast <char*> (&offset), 4);

		char* buffer = new char[max_length + 1];
		for (int i = 0; i < entries_count - 1; ++i) {
			source.read(buffer, offsets[i + 1] - offsets[i]);
			entries.push_back(std::wstring(reinterpret_cast <wchar_t*> (buffer)));
			// Here copying is necessary as the buffer is constantly refreshed
		}
		//The last iteration doesn't have an "i+1" offset
		source.read(buffer, filesize - offsets[entries_count - 1]);
		entries.push_back(std::wstring(reinterpret_cast <wchar_t*> (buffer)));
		delete[] buffer;
		from_file = true;
		original_file = filename;
	}
	MSG::MSG(const fs::path& path) : magic(0), entries_count(0), offsets(), entries(), from_file(false), original_file("") { // TO DO: exception safety
		std::ifstream source(path, std::ios::binary | std::ios::ate);
		if (!source.is_open()) {
			throw std::runtime_error("MSG: can't access " + path.string());
		}

		int filesize = static_cast<int> (source.tellg());
		source.seekg(0);
		source.read(reinterpret_cast <char*> (&entries_count), 4);
		source.read(reinterpret_cast <char*> (&magic), 4);
		int offset;
		int max_length = 0;

		//First iteration doesn't make comparisons
		source.read(reinterpret_cast <char*> (&offset), 4);
		offsets.push_back(offset);

		for (int i = 1; i < entries_count; ++i) {
			source.read(reinterpret_cast <char*> (&offset), 4);
			offsets.push_back(offset);
			if (max_length < offset - offsets[i - 1]) {
				max_length = offset + offsets[i - 1];
			}
		}
		if (max_length < filesize - offsets[entries_count - 1]) {
			max_length = filesize - offsets[entries_count - 1];
		}
		//Skip next 4 zero bytes
		source.read(reinterpret_cast <char*> (&offset), 4);

		char* buffer = new char[max_length + 1];
		for (int i = 0; i < entries_count - 1; ++i) {
			source.read(buffer, offsets[i + 1] - offsets[i]);
			entries.push_back(std::wstring(reinterpret_cast <wchar_t*> (buffer)));
			// Here copying is necessary as the buffer is constantly refreshed
		}
		//The last iteration doesn't have an "i+1" offset
		source.read(buffer, filesize - offsets[entries_count - 1]);
		entries.push_back(std::wstring(reinterpret_cast <wchar_t*> (buffer)));
		delete[] buffer;
		from_file = true;
		original_file = path;
	}

	/*MSG(const std::ifstream& stream) {

	}*/
	int MSG::count() const {
		return entries_count;
	}
	int MSG::getMagic() const {
		return magic;
	}
	void MSG::setMagic(int _magic) {
		magic = _magic;
	}
	std::wstring& MSG::operator[] (size_t index) {
		return entries.at(index);
	}
	std::wstring MSG::operator[] (size_t index) const {
		return entries.at(index);
	}

	void MSG::save_to_file(const std::string& filename, PataponMessageFormat format) {
		std::ofstream dest(filename, std::ios::binary);
		if (!dest.is_open()) {
			throw std::runtime_error("Can't access " + filename);
		}

		if (format == PataponMessageFormat::MSG) {
			dest.write(reinterpret_cast <char*> (&entries_count), 4);
			dest.write(reinterpret_cast <char*> (&magic), 4);

			// recalculate offsets
			offsets.resize(entries_count);
			offsets[0] = 4 * (entries_count + 3);
			dest.write(reinterpret_cast <char*> (&offsets[0]), 4);
			for (int i = 1; i < entries_count; ++i) {
				offsets[i] = offsets[i-1] + sizeof(std::wstring::value_type) * (entries[i - 1].size() + 1);
				dest.write(reinterpret_cast <char*> (&offsets[i]), 4);
			}

			dest.put('\0');
			dest.put('\0');
			dest.put('\0');
			dest.put('\0');

			for (int i = 0; i < entries_count; ++i) {
				dest.write(reinterpret_cast<const char*>(entries[i].c_str()), sizeof(std::wstring::value_type) * (entries[i].size() + 1));
			}
			
		}
		else {
			// BOM
			dest.put(static_cast<char> (0xFF));
			dest.put(static_cast<char> (0xFE));

			std::string settings_str = "SETTINGS:" + std::to_string(magic) + "," + std::to_string(entries_count);
			std::wstring settings_wstr(settings_str.begin(), settings_str.end());
			dest.write(reinterpret_cast<const char*>(settings_wstr.c_str()), sizeof(std::wstring::value_type) * (settings_wstr.size()));

			// EOL
			//dest.put('\0');
			//dest.put('\r');
			//dest.put('\0');
			//dest.put('\n');

		}
	}
	void MSG::save_to_file(const fs::path& path, PataponMessageFormat format) {
		std::ofstream dest(path, std::ios::binary);
		if (!dest.is_open()) {
			throw std::runtime_error("Can't access " + path.string());
		}

		if (format == PataponMessageFormat::MSG) {

		}
		else {

		}
	}
	void MSG::update_source_file() {

	}
}

#endif
