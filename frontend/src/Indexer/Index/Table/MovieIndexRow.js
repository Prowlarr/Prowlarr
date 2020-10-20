import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import { icons, kinds } from 'Helpers/Props';
import DeleteMovieModal from 'Indexer/Delete/DeleteMovieModal';
import EditIndexerModalConnector from 'Settings/Indexers/Indexers/EditIndexerModalConnector';
import translate from 'Utilities/String/translate';
import CapabilitiesLabel from './CapabilitiesLabel';
import ProtocolLabel from './ProtocolLabel';
import styles from './MovieIndexRow.css';

class MovieIndexRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isEditIndexerModalOpen: false,
      isDeleteMovieModalOpen: false
    };
  }

  onEditIndexerPress = () => {
    this.setState({ isEditIndexerModalOpen: true });
  }

  onEditIndexerModalClose = () => {
    this.setState({ isEditIndexerModalOpen: false });
  }

  onDeleteMoviePress = () => {
    this.setState({
      isEditIndexerModalOpen: false,
      isDeleteMovieModalOpen: true
    });
  }

  onDeleteMovieModalClose = () => {
    this.setState({ isDeleteMovieModalOpen: false });
  }

  onUseSceneNumberingChange = () => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
    //
  }

  //
  // Render

  render() {
    const {
      id,
      name,
      enableRss,
      enableAutomaticSearch,
      enableInteractiveSearch,
      protocol,
      privacy,
      added,
      supportsTv,
      supportsBooks,
      supportsMusic,
      supportsMovies,
      columns,
      isMovieEditorActive,
      isSelected,
      onSelectedChange
    } = this.props;

    const {
      isEditIndexerModalOpen,
      isDeleteMovieModalOpen
    } = this.state;

    return (
      <>
        {
          columns.map((column) => {
            const {
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (isMovieEditorActive && column.name === 'select') {
              return (
                <VirtualTableSelectCell
                  inputClassName={styles.checkInput}
                  id={id}
                  key={column.name}
                  isSelected={isSelected}
                  isDisabled={false}
                  onSelectedChange={onSelectedChange}
                />
              );
            }

            if (column.name === 'status') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {
                    enableRss || enableAutomaticSearch || enableInteractiveSearch ?
                      <Label kind={kinds.SUCCESS}>
                        {'Enabled'}
                      </Label>:
                      null
                  }
                  {
                    !enableRss && !enableAutomaticSearch && !enableInteractiveSearch ?
                      <Label
                        kind={kinds.DISABLED}
                        outline={true}
                      >
                        {translate('Disabled')}
                      </Label> :
                      null
                  }
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'name') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {name}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'privacy') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <Label>
                    {privacy}
                  </Label>
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'protocol') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <ProtocolLabel
                    protocol={protocol}
                  />
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'capabilities') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <CapabilitiesLabel
                    supportsBooks={supportsBooks}
                    supportsMovies={supportsMovies}
                    supportsMusic={supportsMusic}
                    supportsTv={supportsTv}
                  />
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'added') {
              return (
                <RelativeDateCellConnector
                  key={column.name}
                  className={styles[column.name]}
                  date={added}
                  component={VirtualTableRowCell}
                />
              );
            }

            if (column.name === 'actions') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <IconButton
                    name={icons.EXTERNAL_LINK}
                    title={'Website'}
                  />

                  <IconButton
                    name={icons.EDIT}
                    title={translate('EditMovie')}
                    onPress={this.onEditIndexerPress}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }

        <EditIndexerModalConnector
          id={id}
          isOpen={isEditIndexerModalOpen}
          onModalClose={this.onEditIndexerModalClose}
        />

        <DeleteMovieModal
          isOpen={isDeleteMovieModalOpen}
          movieId={id}
          onModalClose={this.onDeleteMovieModalClose}
        />
      </>
    );
  }
}

MovieIndexRow.propTypes = {
  id: PropTypes.number.isRequired,
  protocol: PropTypes.string.isRequired,
  privacy: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  enableRss: PropTypes.bool.isRequired,
  enableAutomaticSearch: PropTypes.bool.isRequired,
  enableInteractiveSearch: PropTypes.bool.isRequired,
  supportsTv: PropTypes.bool.isRequired,
  supportsBooks: PropTypes.bool.isRequired,
  supportsMusic: PropTypes.bool.isRequired,
  supportsMovies: PropTypes.bool.isRequired,
  added: PropTypes.string.isRequired,
  tags: PropTypes.arrayOf(PropTypes.number).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSearchingMovie: PropTypes.bool.isRequired,
  isMovieEditorActive: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

export default MovieIndexRow;
